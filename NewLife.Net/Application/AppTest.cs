﻿using System.Diagnostics;
using System.Net;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net.Application;

/// <summary>网络应用程序测试</summary>
public static class AppTest
{
    #region 基础服务测试
    /// <summary>开始测试</summary>
    public static void Start()
    {
        StartServer();
        StartClient();
    }

    /// <summary>开始测试</summary>
    public static void StartServer()
    {
        var ts = new Type[] { typeof(ChargenServer), typeof(DaytimeServer), typeof(DiscardServer), typeof(EchoServer), typeof(TimeServer) };
        //var ts = new Type[] { typeof(EchoServer) };
        var list = new List<NetServer>();
        foreach (var item in ts)
        {
            var server = Activator.CreateInstance(item) as NetServer;
            server.Start();
            list.Add(server);
        }
    }

    /// <summary>开始测试</summary>
    public static void StartClient()
    {
        StartEchoServer(7);
        StartDaytimeServer(13);
        StartTimeServer(37);
        StartDiscardServer(9);
        StartChargenServer(19);
    }

    static AutoResetEvent _are = new AutoResetEvent(true);
    static void OnReceived(Object sender, ReceivedEventArgs e)
    {
        var session = sender as ISocketSession;
        Console.WriteLine("客户端{0} 收到 [{1}]: {2}", session, e.Packet.Total, e.Packet.ToStr());

        _are.Set();
    }

    static void OnError(Object sender, ExceptionEventArgs e)
    {
        Console.WriteLine("客户端{0}错误 {1}", sender, e.Exception);
    }

    static void TestSend(String name, NetUri uri, Boolean isAsync, Boolean isSendData, Boolean isReceiveData)
    {
        Console.WriteLine();

        var msg = String.Format("{0}Test_{1}_{2}!", name, uri.Type, isAsync ? "异步" : "同步");
        var client = uri.CreateRemote();
        client.Error += OnError;
        if (isAsync && isReceiveData)
        {
            _are.Reset();
            client.Received += OnReceived;
            client.Open();
        }
        if (isSendData) client.Send(msg);

        var rs = false;
        if (isReceiveData)
        {
            if (!isAsync)
            {
                try
                {
                    //Console.WriteLine("客户端" + session + " " + session.ReceiveString());
                    var pk = client.Receive();
                    Console.WriteLine("客户端{0} 收到 [{1}]: {2}", client, pk.Total, pk.ToStr());

                    rs = true;
                }
                catch (Exception ex)
                {
                    Debug.Fail("同步超时！" + ex.Message);
                }
            }
            else
            {
                if (!_are.WaitOne(2000))
                    Debug.Fail("异步超时！");
                else
                    rs = true;
            }
        }
        client.Dispose();
        client = null;
        GC.Collect();

        XTrace.WriteLine(rs ? "成功！" : "失败！");
    }

    static void TestSends(String name, IPEndPoint ep, Boolean isSendData, Boolean isReceiveData = true)
    {
        //if (ep.AddressFamily == AddressFamily.InterNetworkV6) return;

        Console.WriteLine();
        Console.WriteLine("{0}：", name);
        //TestSend(name, ProtocolType.Udp, ep, false, isSendData, isReceiveData);
        //TestSend(name, ProtocolType.Udp, ep, true, isSendData, isReceiveData);
        //TestSend(name, ProtocolType.Tcp, ep, false, isSendData, isReceiveData);
        //TestSend(name, ProtocolType.Tcp, ep, true, isSendData, isReceiveData);
        var uri = new NetUri(NetType.Udp, ep);
        TestSend(name, uri, false, isSendData, isReceiveData);
        TestSend(name, uri, true, isSendData, isReceiveData);
        uri.Type = NetType.Tcp;
        TestSend(name, uri, false, isSendData, isReceiveData);
        TestSend(name, uri, true, isSendData, isReceiveData);

        GC.Collect();
    }

    static void StartEchoServer(Int32 port)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        TestSends("Echo", ep, true);

        ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

        TestSends("Echo IPv6", ep, true);
    }

    static void StartDaytimeServer(Int32 port)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        TestSends("Daytime", ep, true);

        ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

        TestSends("Daytime IPv6", ep, true);
    }

    static void StartTimeServer(Int32 port)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        TestSends("Time", ep, true);

        ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

        TestSends("Time IPv6", ep, true);
    }

    static void StartDiscardServer(Int32 port)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        TestSends("Discard", ep, true, false);

        ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

        TestSends("Discard IPv6", ep, true, false);
    }

    static void StartChargenServer(Int32 port)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);

        TestSends("Chargen", ep, true);

        ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

        TestSends("Chargen IPv6", ep, true);
    }
    #endregion

    #region TCP大量连接测试
    /// <summary>TCP大量连接测试</summary>
    public static void TcpConnectionTest()
    {
        while (true)
        {
            Console.Write("请选择模式：（1，服务端 2，客户端）");
            var str = Console.ReadLine();

            if (str == "1")
            {
                TestServer();
                break;
            }
            else if (str == "2")
            {
                TestClient();
                break;
            }
        }
    }

    static NetServer server = null;
    static void TestServer()
    {
        var port = ReadInt("请输入监听端口：", 1, 65535);

        // 扩大事件池
        //NetEventArgs.Pool.Max = 200000;

        server = new NetServer();
        server.ProtocolType = NetType.Tcp;
        server.Port = port;
        server.UseSession = true;
        server.Received += server_Received;
        //// 最大不活跃时间设为10分钟
        //foreach (var item in server.Servers)
        //{
        //    item.SessionTimeout = 10 * 60;
        //}
        server.Start();
        server.Log = null;

        Task.Factory.StartNew(ShowStatus);

        Console.WriteLine("服务端准备就绪，任何时候任意键退出服务程序！");
        Console.ReadKey(true);

        server.Dispose();
    }

    static void server_Received(Object sender, ReceivedEventArgs e)
    {
        OnReceive(sender as ISocketSession, e.Packet.GetStream());
    }

    /// <summary>已重载。</summary>
    /// <param name="session"></param>
    /// <param name="stream"></param>
    static void OnReceive(ISocketSession session, Stream stream)
    {
        if (stream.Length == 0) return;

        asyncCount++;

        //if (stream.Length > 100)
        //    Console.WriteLine("Echo {0} [{1}]", session.Remote, stream.Length);
        //else
        //    Console.WriteLine("Echo {0} [{1}] {2}", session.Remote, stream.Length, e.GetString());
        //var p = stream.Position;
        //var msg = "";
        //if (stream.Length > 100)
        //    msg = String.Format("Echo {0} [{1}]", session.Remote, stream.Length);
        //else
        //    msg = String.Format("Echo {0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());

        ////session.Send(e.Buffer, e.Offset, stream.Length);
        //stream.Position = p;
        session.Send(stream);
    }

    static Int32 asyncCount = 0;
    static void ShowStatus()
    {
        while (true)
        {
            try
            {
                ThreadPool.GetAvailableThreads(out var wt, out var cpt);
                var threads = Process.GetCurrentProcess().Threads.Count;

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                //Console.WriteLine("异步:{0} 会话:{1} Thread:{2}/{3}/{4} Pool:{5}/{6}/{7}", asyncCount, server.Sessions.Count, threads, wt, cpt, pool.StockCount, pool.FreeCount, pool.CreateCount);
                //var count = server.Sessions.Count;
                var count = server.SessionCount;
                Console.WriteLine("消息:{0} 会话:{1} Thread:{2}/{3}/{4}", asyncCount, count, threads, wt, cpt);
                Console.ForegroundColor = color;
            }
            catch { }

            Thread.Sleep(3000);
        }
    }

    //static Thread[] threads;
    static void TestClient()
    {
        Console.Write("请输入服务器地址：");
        var host = Console.ReadLine();
        var port = ReadInt("请输入服务器端口：", 1, 65535);

        var ep = NetHelper.ParseEndPoint(host, port);
        var uri = new NetUri(NetType.Tcp, ep);

        Console.WriteLine("开始测试连接{0}……", uri);

        var session = uri.CreateRemote();
        session.Send("Hi");
        var rs = session.ReceiveString();
        session.Dispose();
        if (rs.IsNullOrWhiteSpace())
        {
            Console.WriteLine("连接失败！");
            return;
        }

        var threadcount = ReadInt("请输入线程数（建议20）：", 1, 10000);
        var perthread = ReadInt("请输入每线程连接数（建议500）：", 1, 10000);
        //var time = ReadInt("请输入连接间隔（毫秒，建议10毫秒）：", 1, 10000);
        var time = 10;

        var threads = new Thread[threadcount];
        for (var i = 0; i < threadcount; i++)
        {
            var th = new Thread(ClientProcess);
            th.IsBackground = true;
            th.Priority = ThreadPriority.BelowNormal;
            th.Name = "Client_" + (i + 1);

            threads[i] = th;
            var p = new TCParam() { ID = i + 1, Count = perthread, Period = time, Uri = uri };
            th.Start(p);

            Thread.Sleep(100);
        }
    }

    static void ClientProcess(Object state)
    {
        var p = state as TCParam;

        var msg = String.Format("Hi I am {0}!", p.ID);

        var clients = new ISocketClient[p.Count];
        for (var k = 0; k < 100; k++)
        {
            Console.WriteLine("第{1}轮处理：{0}", p.ID, k + 1);

            for (var i = 0; i < p.Count; i++)
            {
                try
                {
                    var client = clients[i];
                    if (client == null || client.Disposed)
                    {
                        client = p.Uri.CreateRemote();

                        clients[i] = client;
                    }

                    client.Send(msg);
                }
                catch { }

                if (p.Period > 0) Thread.Sleep(p.Period);
            }
        }
    }

    class TCParam
    {
        public Int32 ID;
        public Int32 Count;
        public Int32 Period;

        public NetUri Uri;
    }

    static Int32 ReadInt(String title, Int32 min, Int32 max)
    {
        if (String.IsNullOrEmpty(title))
            title = "请输入数字：";
        else if (title[title.Length - 1] != '：')
            title += "：";

        var n = 0;
        while (n < min || n > max)
        {
            Console.Write(title);
            var str = Console.ReadLine();
            if (!String.IsNullOrEmpty(str)) Int32.TryParse(str, out n);
        }
        return n;
    }
    #endregion
}