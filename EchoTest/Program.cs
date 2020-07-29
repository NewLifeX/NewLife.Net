using System;
using System.Threading;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;

namespace EchoTest
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
        static void TestServer()
        {
            // 实例化服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
            var svr = new MyNetServer
            {
                Port = 1234,
                Log = XTrace.Log,
                SessionLog = XTrace.Log,
                StatPeriod = 30,
                Tracer = new DefaultTracer { Period = 15, Log = XTrace.Log },
#if DEBUG
                SocketLog = XTrace.Log,
                LogSend = true,
                LogReceive = true,
#endif
            };
            svr.Start();

            _server = svr;

            // 定时显示性能数据
            _timer = new TimerX(ShowStat, svr, 100, 1000) { Async = true };
        }

        static void TestClient()
        {
            var uri = new NetUri("tcp://::1:1234");
            //var uri = new NetUri("tcp://net.newlifex.com:1234");
            var client = uri.CreateRemote();
            client.Log = XTrace.Log;
            client.LogSend = true;
            client.LogReceive = true;
            client.Received += (s, e) =>
            {
                XTrace.WriteLine("收到：{0}", e.Packet.ToStr());
            };
            client.Open();

            // 循环发送数据
            for (var i = 0; i < 5; i++)
            {
                Thread.Sleep(1000);

                var str = "你好" + (i + 1);
                client.Send(str);
            }

            client.Dispose();
        }

        static void ShowStat(Object state)
        {
            var msg = "";
            if (state is NetServer ns)
                msg = ns.GetStat();
            //else if (state is ISocketRemote ss)
            //    msg = ss.GetStat();

            if (!msg.IsNullOrEmpty()) Console.Title = msg;
        }
    }
}