using System;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Remoting;
using NewLife.Threading;

namespace RpcTest
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
        static ApiServer _server;
        static void TestServer()
        {
            // 实例化RPC服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
            var svr = new ApiServer(1234);
            // 注册服务控制器
            svr.Register<MyController>();
            svr.Register<UserController>();

            // 指定编码器
            svr.Encoder = new JsonEncoder();
            svr.EncoderLog = XTrace.Log;

            // 打开原始数据日志
            var ns =  svr.EnsureCreate() as NetServer;
            //var ns = svr.Server as NetServer;
            ns.Log = XTrace.Log;
            ns.LogSend = true;
            ns.LogReceive = true;

            svr.Log = XTrace.Log;
            svr.Start();

            _server = svr;

            // 定时显示性能数据
            _timer = new TimerX(ShowStat, ns, 100, 1000);
        }

        static async void TestClient()
        {
            var client = new MyClient("tcp://127.0.0.1:1234");

            // 指定编码器
            client.Encoder = new JsonEncoder();
            client.EncoderLog = XTrace.Log;

            //// 打开原始数据日志
            //var ns = client.Client;
            //ns.Log = XTrace.Log;
            //ns.LogSend = true;
            //ns.LogReceive = true;

            // 定时显示性能数据
            client.StatPeriod = 5;

            client.Log = XTrace.Log;
            client.Open();

            // 定时显示性能数据
            //_timer = new TimerX(ShowStat, ns, 100, 1000);

            // 标准服务，Json
            var n = await client.AddAsync(1245, 3456);
            XTrace.WriteLine("Add: {0}", n);

            // 高速服务，二进制
            var buf = "Hello".GetBytes();
            var pk = await client.RC4Async(buf);
            XTrace.WriteLine("RC4: {0}", pk.ToHex());

            // 返回对象
            var user = await client.FindUserAsync(123, true);
            XTrace.WriteLine("FindUser: ID={0} Name={1} Enable={2} CreateTime={3}", user.ID, user.Name, user.Enable, user.CreateTime);

            // 拦截异常
            try
            {
                user = await client.FindUserAsync(123, true);
            }
            catch (ApiException ex)
            {
                XTrace.WriteLine("FindUser出错，错误码={0}，内容={1}", ex.Code, ex.Message);
            }
        }

        static void ShowStat(Object state)
        {
            var msg = "";
            if (state is NetServer ns)
                msg = ns.GetStat();
            else if (state is ISocketRemote ss)
                msg = ss.GetStat();

            Console.Title = msg;
        }
    }
}
