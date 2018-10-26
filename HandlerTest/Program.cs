using System;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Threading;

namespace HandlerTest
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            try
            {
                Console.Write("请选择运行模式：1，服务端；2，客户端  ");
                var ch = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (ch == '1')
                    TestServer();
                else
                    TestClient();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Console.WriteLine("OK!");
            Console.ReadKey();
        }

        static TimerX _timer;
        static NetServer _server;
        static PerfCounter _counter;
        static void TestServer()
        {
            _counter = new PerfCounter();

            // 实例化服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
            var svr = new NetServer
            {
                Port = 1234,
                Log = XTrace.Log
            };
            //svr.Add(new LengthFieldCodec { Size = 4 });
            svr.Add<StandardCodec>();
            //svr.Add<EchoHandler>();
            svr.Add(new EchoHandler { Counter = _counter });

#if DEBUG
            // 打开原始数据日志
            var ns = svr.Server;
            ns.Log = XTrace.Log;
            ns.LogSend = true;
            ns.LogReceive = true;
#endif

            svr.Start();

            _server = svr;

            // 定时显示性能数据
            _timer = new TimerX(ShowStat, svr, 100, 1000) { Async = true };
        }

        static void TestClient()
        {
            var uri = new NetUri("tcp://127.0.0.1:1234");
            //var uri = new NetUri("tcp://net.newlifex.com:1234");
            var client = uri.CreateRemote();
            client.Log = XTrace.Log;
            client.Received += (s, e) =>
            {
                var pk = e.Message as Packet;
                XTrace.WriteLine("收到：{0}", pk.ToStr());
            };
            //client.Add(new LengthFieldCodec { Size = 4 });
            client.Add<StandardCodec>();

            // 打开原始数据日志
            var ns = client;
            ns.LogSend = true;
            ns.LogReceive = true;

            client.Open();

            // 定时显示性能数据
            _timer = new TimerX(ShowStat, client, 100, 1000) { Async = true };

            // 循环发送数据
            for (var i = 0; i < 5; i++)
            {
                var str = "你好" + (i + 1);
                var pk = new Packet(str.GetBytes());
                client.SendMessageAsync(pk);
            }
        }

        class User
        {
            public Int32 ID { get; set; }
            public String Name { get; set; }
        }

        private static String _last;
        static void ShowStat(Object state)
        {
            var msg = "";
            if (state is NetServer ns)
                msg = "处理：" + _counter + " " + ns.GetStat();
            else if (state is ISocketRemote ss)
                msg = ss.GetStat();

            if (msg == _last) return;
            _last = msg;

            //Console.Title = msg;
            XTrace.WriteLine(msg);
        }
    }
}