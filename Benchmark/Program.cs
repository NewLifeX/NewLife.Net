using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                XTrace.WriteException(ex.GetTrue());
            }

            //Console.WriteLine("OK!");
            //Console.ReadKey();
        }

        static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("压力测试工具：nc [-c 100] [-n 10000] [-s content] tcp://127.0.0.1:1234");
            Console.WriteLine("\t-c\t并发数。默认100用户");
            Console.WriteLine("\t-n\t请求数。默认每用户请求10000次");
            Console.WriteLine("\t-s\t字符串内容。支持0x开头十六进制");

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
            Console.WriteLine("NewLife.NC v{0}", AssemblyX.Entry.Version);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("目标：{0}", uri);
            Console.WriteLine("请求：{0:n0}", cfg.Times);
            Console.WriteLine("并发：{0:n0}", cfg.Thread);
            Console.WriteLine("内容：[{0:n0}] {1}", pk.Count, txt);
            Console.ResetColor();
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            // 多线程
            var ts = new List<Task>();
            var total = 0;
            for (var i = 0; i < cfg.Thread; i++)
            {
                var tsk = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var client = uri.CreateRemote();
                        client.Open();
                        for (var k = 0; k < cfg.Times; k++)
                        {
                            client.Send(pk);
                            Interlocked.Increment(ref total);
                        }
                        return client;
                    }
                    catch { return null; }
                }, TaskCreationOptions.LongRunning);
                ts.Add(tsk);
            }
            Task.WaitAll(ts.ToArray());

            sw.Stop();

            Console.WriteLine("完成：{0:n0}", total);

            var ms = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("速度：{0:n0}tps", total * 1000L / ms);

            //Thread.Sleep(5000);
            Console.ReadKey(true);
        }
    }
}