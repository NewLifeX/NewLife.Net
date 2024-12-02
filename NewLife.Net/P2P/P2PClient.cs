﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NewLife.Data;
using NewLife.Net.Sockets;
using NewLife.Security;

namespace NewLife.Net.P2P
{
    /// <summary>P2P客户端</summary>
    /// <remarks>
    /// Tcp打洞流程（A想连接B）：
    /// 1，客户端A通过路由器NAT-A连接打洞服务器S
    /// 2，A向S发送标识，异步等待响应
    /// 3，S记录A的标识和会话<see cref="ISocketClient"/>
    /// 3，客户端B，从业务通道拿到标识
    /// 4，B通过路由器NAT-B连接打洞服务器S，异步等待响应
    /// 5，B向S发送标识
    /// 6，S找到匹配标识，同时向AB会话响应对方的外网地址，会话结束
    /// 7，AB收到响应，B先连接A，A暂停一会后连接B
    /// 
    /// 经鉴定，我认为网络上所有关于TCP穿透的文章，全部都是在胡扯
    /// 不外乎几种可能：
    /// 1，双方都在同一个内网
    /// 2，通过服务器中转所有数据
    /// 3，臆断，认为那样子就可行。包括许多论文也是这个说法，我中的这招，不经过NAT会成功，经过最流行的TP-LINK就无法成功
    /// </remarks>
    public class P2PClient : Netbase
    {
        #region 属性
        private ISocketServer _Server;
        /// <summary>客户端</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private IPEndPoint _HoleServer;
        /// <summary>打洞服务器地址</summary>
        public IPEndPoint HoleServer { get { return _HoleServer; } set { _HoleServer = value; } }

        private ISocketClient _Client;
        /// <summary>客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }

        private NetType _ProtocolType = NetType.Udp;
        /// <summary>协议</summary>
        public NetType ProtocolType { get { return _ProtocolType; } set { _ProtocolType = value; } }

        private IPEndPoint _ParterAddress;
        /// <summary>目标伙伴地址</summary>
        public IPEndPoint ParterAddress { get { return _ParterAddress; } set { _ParterAddress = value; } }

        private Int32 _Success;
        /// <summary>是否成功</summary>
        public Int32 Success { get { return _Success; } set { _Success = value; } }
        #endregion

        #region 方法
        /// <summary></summary>
        public void EnsureServer()
        {
            if (Server == null)
            {
                if (ProtocolType == NetType.Tcp)
                {
                    var server = new TcpServer();
                    Server = server;
                    //server.ReuseAddress = true;
                    server.NewSession += server_Accepted;
                }
                else
                {
                    var server = new UdpServer();
                    //Server = server;
                    //server.ReuseAddress = true;
                    server.Received += server_Received;
                    //server.Bind();
                    server.Open();
                }

                Server.Start();

                WriteLog("监听：{0}", Server);
            }
        }

        void server_Received(Object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;

            var str = e.Packet.ToStr();
            var remote = "" + session.Remote.EndPoint;
            if (remote == "" + HoleServer)
            {
                WriteLog("HoleServer数据到来：{0} {1}", session.Remote, str);

                var ss = str.Split(":");
                if (ss == null || ss.Length < 2) return;

                if (!IPAddress.TryParse(ss[0], out var address)) return;
                if (!Int32.TryParse(ss[1], out var port)) return;
                var ep = new IPEndPoint(address, port);
                ParterAddress = ep;

                Console.WriteLine("准备连接对方：{0}", ep);
                while (Success <= 0)
                {
                    (Server as UdpServer).Client.Send("Hello!", null, ep);

                    Thread.Sleep(100);
                    if (Success > 0) break;
                    Thread.Sleep(3000);
                }
            }
            else if (remote == "" + ParterAddress)
            {
                WriteLog("Parter数据到来：{0} {1}", session.Remote, str);
                //Success = true;
                if (Success > 0) Success++;

                //var session = e.Session;
                if (session != null)
                {
                    session.Send("P2P连接已建立！", null);
                    WriteLog("P2P连接已建立！");
                    session.Send("我与" + session.Remote + "的P2P连接已建立！", null);

                    while (true)
                    {
                        Console.Write("请输入要说的话：");
                        var line = Console.ReadLine();
                        if (String.IsNullOrEmpty(line)) continue;
                        if (line == "exit") break;

                        session.Send(line, null);
                        Console.WriteLine("已发送！");
                    }
                }
            }
            else
            {
                WriteLog("未识别的数据到来：{0} {1}", session.Remote, str);
            }
        }

        void server_Accepted(Object sender, SessionEventArgs e)
        {
            var session = e.Session as ISocketSession;
            WriteLog("连接到来：{0}", session.Remote);

            if (session != null)
            {
                session.Received += client_Received;
                session.Send("P2P连接已建立！");
                WriteLog("P2P连接已建立！");
            }
        }

        void EnsureClient()
        {
            EnsureServer();
            if (Client == null)
            {
                var server = Server;

                var client = new TcpSession();
                Client = client;
                //client.Address = server.LocalEndPoint.Address;
                //client.Port = server.LocalEndPoint.Port;
                //client.ReuseAddress = true;
                //client.Connect(HoleServer);
                //var session = client.CreateSession();
                var session = client as ISocketSession;
                session.Received += client_Received;
                client.Open();
            }
        }

        void client_Received(Object sender, ReceivedEventArgs e)
        {
            //WriteLog("数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());

            //var ss = e.GetString().Split(":");
            var ss = e.Packet.ToStr().Split(":");
            if (ss == null || ss.Length < 2) return;

            if (!IPAddress.TryParse(ss[0], out var address)) return;
            if (!Int32.TryParse(ss[1], out var port)) return;
            var ep = new IPEndPoint(address, port);
            ParterAddress = ep;

            Client.Dispose();
            var server = Server;

            //Random rnd = new Random((Int32)DateTime.Now.Ticks);
            Thread.Sleep(Rand.Next(0, 2000));

            var client = new TcpSession();
            Client = client;
            //client.Address = server.LocalEndPoint.Address;
            //client.Port = server.LocalEndPoint.Port;
            //client.ReuseAddress = true;
            client.Local.EndPoint = server.Local.EndPoint;
            Console.WriteLine("准备连接对方：{0}", ep);
            try
            {
                //client.Connect(ep);
                client.Received += client_Received2;
                client.Open();

                Client.Send("Hello!");
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        void client_Received2(Object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            WriteLog("数据到来2：{0} {1}", session.Remote, e.Packet.ToStr());
        }
        #endregion

        #region 业务
        /// <summary>开始处理</summary>
        /// <param name="name">名称</param>
        public void Start(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            //if (ProtocolType == ProtocolType.Tcp) EnsureClient();

            SendToHole("reg:" + name);

            //while (ParterAddress == null)
            //{
            //    Server.Send("reg:" + name, null, HoleServer);
            //    var ep = new IPEndPoint(HoleServer.Address, HoleServer.Port + 1);
            //    Server.Send("checknat", null, ep);

            //    Thread.Sleep(100);
            //    if (ParterAddress != null) break;
            //    Thread.Sleep(19000);
            //}
        }

        void SendToHole(String msg)
        {
            var ep = new IPEndPoint(HoleServer.Address, HoleServer.Port + 1);
            EnsureServer();
            var server = Server as UdpServer;
            if (server != null)
            {
                server.Client.Send(msg, null, HoleServer);
                //server.Send("test", null, HoleServer);
                if (msg.StartsWith("reg"))
                {
                    server.Client.Send("checknat", null, ep);
                }
            }
            else
            {
                var client = new TcpSession() as ISocketSession;
                //client.Address = Server.LocalEndPoint.Address;
                //client.Port = Server.LocalEndPoint.Port;
                //client.ReuseAddress = true;
                //client.Connect(ep);
                client.Local.EndPoint = Server.Local.EndPoint;
                client.Send("checknat");
                WriteLog("HoleServer数据到来：{0}", client.ReceiveString());
                client.Dispose();

                EnsureClient();
                //Client.Send(msg, null);
                Client.Send(msg);
            }
        }
        #endregion
    }
}