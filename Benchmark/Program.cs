using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;

namespace Benchmark
{
    internal class Program
    {
        private static void Main(String[] args)
        {
            XTrace.UseConsole();

            try
            {
                var cfg = new Config();

                // 分解参数
                if (args != null && args.Length > 0) cfg.Parse(args);

                // 显示帮助菜单或执行
                if (cfg.Address.IsNullOrEmpty())
                    ShowHelp();
                else
                    Work(cfg);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            //Console.WriteLine("OK!");
            //Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("压力测试工具：netbench [-c 100] [-n 10000] [-i 0] [-s content] tcp://127.0.0.1:1234");
            Console.WriteLine("\t-c\t并发数。默认100用户");
            Console.WriteLine("\t-n\t请求数。默认每用户请求10000次");
            Console.WriteLine("\t-i\t间隔。间隔多少毫秒发一次请求");
            Console.WriteLine("\t-r\t等待响应。");
            Console.WriteLine("\t-s\t字符串内容。支持0x开头十六进制");
            Console.WriteLine("\t-b\t绑定的本地地址，*表示每一个分开绑定，支持输入一段 10.0.0.31-40。");

            Console.WriteLine();
            Console.WriteLine("本地IP地址：{0}", NetHelper.GetIPsWithCache().Join("\r\n\t"));

            Console.ResetColor();
        }

        private static void Work(Config cfg)
        {
            var uri = new NetUri(cfg.Address);
            var txt = cfg.Content;
            if (txt.IsNullOrEmpty()) txt = cfg.Content = "学无先后达者为师";

            var buf = txt.StartsWith("0x") ? txt.TrimStart("0x").ToHex() : txt.GetBytes();
            var pk = new Packet(buf);

            // 绑定集合
            var binds = new List<(IPAddress, NetUri)>();
            if (!cfg.Bind.IsNullOrEmpty())
            {
                // 如果监听的是本地地址，则可以使用所有本地IP，但是需要根据类型修改uri
                if (uri.Address.IsLocal())
                {
                    foreach (var item in cfg.GetBinds())
                    {
                        // 修改为相同协议栈
                        var uri2 = new NetUri(uri.Type, item, uri.Port);
                        binds.Add((item, uri2));
                    }
                }
                else
                {
                    foreach (var item in cfg.GetBinds())
                    {
                        if (item.AddressFamily == uri.Address.AddressFamily && !IPAddress.IsLoopback(item)) binds.Add((item, uri));
                    }
                }
            }
            else
            {
                binds.Add((null, uri));
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NewLife.Benchmark v{0}", AssemblyX.Entry.Version);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("目标：{0}", uri);
            Console.WriteLine("请求：{0:n0}", cfg.Times);
            Console.WriteLine("并发：{0:n0}", cfg.ConcurrentLevel);
            Console.WriteLine("内容：[{0:n0}] {1}", pk.Count, txt);

            if (cfg.Interval > 0) Console.WriteLine("间隔：{0:n0}", cfg.Interval);
            if (!cfg.Bind.IsNullOrEmpty())
            {
                Console.WriteLine("绑定：{0}", cfg.Bind);
                Console.WriteLine("可用：{0}", binds.Join(",", e => e.Item1));
            }

            Console.ResetColor();
            Console.WriteLine();

            _Counter = new PerfCounter();
            _Timer = new TimerX(ShowStat, null, 3_000, 5_000) { Async = true };
            var sw = Stopwatch.StartNew();

            // 每个IP在用端口，非常重要，必须为每个ip绑定指定自己的端口序列，否则操作系统可能让多ip共用一个端口序列，导致最多只能连接6w多
            var ports = new Int32[binds.Count];
            for (var i = 0; i < ports.Length; i++)
            {
                ports[i] = 10000;
            }

            // 多线程
            var ts = new List<Task<Int32>>();
            for (var i = 0; i < cfg.ConcurrentLevel; i++)
            {
                var bind = binds[i % binds.Count];
                var endPoint = bind.Item1 == null ? null : new IPEndPoint(bind.Item1, ports[i % binds.Count]++);
                var tsk = Task.Run(async () => await WorkOneAsync(endPoint, bind.Item2, cfg, pk));
                ts.Add(tsk);

                // 必须控制速度，否则服务端拒绝连接
                //if (i > 0 && i % 100 == 0) Thread.Sleep(100);
            }

            Console.WriteLine("{0:n0} 个并发已就绪", ts.Count);
            var total = Task.WhenAll(ts.ToArray()).Result.Sum();

            sw.Stop();

            Console.WriteLine("完成：{0:n0}", total);

            var ms = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("速度：{0:n0}tps", total * 1000L / ms);
        }

        private static readonly ConcurrentHashSet<Type> _LastErrors = new ConcurrentHashSet<Type>();

        private static async Task<Int32> WorkOneAsync(IPEndPoint local, NetUri uri, Config cfg, Packet pk)
        {
            var count = 0;
            try
            {
                Interlocked.Increment(ref _SessionCount);

                var client = uri.CreateRemote();
                if (cfg.Reply) (client as SessionBase).MaxAsync = 0;
                if (local != null) client.Local.EndPoint = local;

                client.Timeout = 30_000;
                client.Open();

                await Task.Yield();
                for (var k = 0; k < cfg.Times; k++)
                {
                    var ticks = _Counter.StartCount();

                    client.Send(pk);

                    if (cfg.Reply)
                    {
                        var pk2 = client.Receive();
                        if (pk2.Count > 0) count++;
                    }
                    else
                    {
                        count++;
                    }

                    _Counter.StopCount(ticks);

                    if (cfg.Interval > 0)
                        await Task.Delay(cfg.Interval);
                    else
                        await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                if (_LastErrors.TryAdd(ex.GetType()))
                {
                    XTrace.WriteLine("{0}=>{1}", local, uri);
                    XTrace.WriteException(ex);
                }
            }

            Interlocked.Decrement(ref _SessionCount);

            return count;
        }

        private static Int32 _SessionCount;
        private static ICounter _Counter;
        private static TimerX _Timer;
        private static String _LastStat;

        private static void ShowStat(Object state)
        {
            var str = _Counter.ToString();
            if (_LastStat == str) return;
            _LastStat = str;

            XTrace.WriteLine("连接：{0:n0} 收发：{1}", _SessionCount, str);
        }
    }
}