using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

namespace Benchmark
{
    class Program
    {
        static void Main(String[] args)
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

        static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("压力测试工具：netbench [-c 100] [-n 10000] [-i 0] [-s content] tcp://127.0.0.1:1234");
            Console.WriteLine("\t-c\t并发数。默认100用户");
            Console.WriteLine("\t-n\t请求数。默认每用户请求10000次");
            Console.WriteLine("\t-i\t间隔。间隔多少毫秒发一次请求");
            Console.WriteLine("\t-r\t等待响应。");
            Console.WriteLine("\t-s\t字符串内容。支持0x开头十六进制");

            Console.WriteLine();
            Console.WriteLine("本地IP地址：{0}", NetHelper.GetIPsWithCache().Join());

            Console.ResetColor();
        }

        static void Work(Config cfg)
        {
            var uri = new NetUri(cfg.Address);
            var txt = cfg.Content;
            if (txt.IsNullOrEmpty()) txt = cfg.Content = "学无先后达者为师";

            var buf = txt.StartsWith("0x") ? txt.TrimStart("0x").ToHex() : txt.GetBytes();
            var pk = new Packet(buf);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NewLife.Benchmark v{0}", AssemblyX.Entry.Version);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("目标：{0}", uri);
            Console.WriteLine("请求：{0:n0}", cfg.Times);
            Console.WriteLine("并发：{0:n0}", cfg.Thread);
            Console.WriteLine("内容：[{0:n0}] {1}", pk.Count, txt);

            if (cfg.Interval > 0) Console.WriteLine("间隔：{0:n0}", cfg.Interval);
            if (!cfg.Bind.IsNullOrEmpty()) Console.WriteLine("绑定：{0}", cfg.Bind);

            Console.ResetColor();
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            // 多线程
            var ts = new List<Task<Int32>>();
            var maxCPU = Environment.ProcessorCount * 2;
            for (var i = 0; i < cfg.Thread; i++)
            {
                if (cfg.Thread <= maxCPU)
                {
                    var tsk = Task.Factory.StartNew(() => WorkOne(uri, cfg, pk), TaskCreationOptions.LongRunning);
                    ts.Add(tsk);
                }
                else
                {
                    var index = i;
                    var tsk = Task.Run(async () => await WorkOneAsync(index, uri, cfg, pk));
                    ts.Add(tsk);
                }
            }

            Console.WriteLine("{0:n0} 个并发已就绪", ts.Count);
            var total = Task.WhenAll(ts.ToArray()).Result.Sum();

            sw.Stop();

            Console.WriteLine("完成：{0:n0}", total);

            var ms = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("速度：{0:n0}tps", total * 1000L / ms);

            //Thread.Sleep(5000);
            //Console.ReadKey(true);
        }

        static Int32 WorkOne(NetUri uri, Config cfg, Packet pk)
        {
            var count = 0;
            try
            {
                var client = uri.CreateRemote();
                if (cfg.Reply) (client as SessionBase).MaxAsync = 0;
                client.Open();
                for (var k = 0; k < cfg.Times; k++)
                {
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

                    if (cfg.Interval > 0) Thread.Sleep(cfg.Interval);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            return count;
        }

        static async Task<Int32> WorkOneAsync(Int32 index, NetUri uri, Config cfg, Packet pk)
        {
            // 如果有绑定本地地址，直接使用；如果绑定*，则轮流使用
            IPAddress remote = null;
            if (!cfg.Bind.IsNullOrEmpty())
            {
                // 如果监听的是本地地址，则可以使用所有本地IP，但是需要根据类型修改uri
                if (uri.Address.IsLocal())
                {
                    var binds = cfg.GetBinds();

                    if (binds.Length == 1)
                        remote = binds[0];
                    else if (binds.Length > 1)
                        remote = binds[index % binds.Length];

                    // 修改为相同协议栈
                    var addr = remote.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
                    uri = new NetUri(uri.Type, addr, uri.Port);
                }
                else
                {
                    var binds = cfg.GetBinds().Where(e => e.AddressFamily == uri.Address.AddressFamily).ToArray();

                    if (binds.Length == 1)
                        remote = binds[0];
                    else if (binds.Length > 1)
                        remote = binds[index % binds.Length];
                }
            }

            var count = 0;
            try
            {
                var client = uri.CreateRemote();
                if (cfg.Reply) (client as SessionBase).MaxAsync = 0;
                if (remote != null) client.Local.Address = remote;

                client.Open();

                await Task.Yield();
                for (var k = 0; k < cfg.Times; k++)
                {
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

                    if (cfg.Interval > 0)
                        await Task.Delay(cfg.Interval);
                    else
                        await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            return count;
        }
    }
}