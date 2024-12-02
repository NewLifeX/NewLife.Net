﻿using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Net.Stun;

/// <summary>Stun服务端。Simple Traversal of UDP over NATs，NAT 的UDP简单穿越。RFC 3489</summary>
/// <remarks>
/// <a target="_blank" href="http://baike.baidu.com/view/884586.htm">STUN</a>
/// </remarks>
public class StunServer : NetServer
{
    #region 属性
    private IDictionary<Int32, IPEndPoint> _Public;
    /// <summary>我的公网地址。因为当前服务器可能在内网中，需要调用StunClient拿公网地址</summary>
    public IDictionary<Int32, IPEndPoint> Public { get { return _Public; } private set { _Public = value; } }

    private IPEndPoint _Partner;
    /// <summary>伙伴地址。需要改变地址时，向该伙伴地址发送信息</summary>
    public IPEndPoint Partner { get { return _Partner; } set { _Partner = value; } }

    private Int32 _Port2;
    /// <summary>第二端口</summary>
    public Int32 Port2 { get { return _Port2; } set { _Port2 = value; } }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public StunServer()
    {
        Name = GetType().Name.TrimEnd("Server");

        Port = 3478;
        Port2 = Port + 1;

        //// 同时两个端口
        //AddServer(IPAddress.Any, 3478, ProtocolType.Udp, AddressFamily.InterNetwork);
        //AddServer(IPAddress.Any, 3479, ProtocolType.Udp, AddressFamily.InterNetwork);
    }

    /// <summary>确保建立服务器</summary>
    public override void EnsureCreateServer()
    {
        if (Servers.Count <= 0)
        {
            // 同时两个端口
            AddServer(IPAddress.Any, Port, NetType.Unknown, AddressFamily.InterNetwork);
            AddServer(IPAddress.Any, Port2, NetType.Unknown, AddressFamily.InterNetwork);

            var dic = new Dictionary<Int32, IPEndPoint>();
            IPEndPoint ep = null;
            var pub = NetHelper.MyIP();
            StunResult rs = null;
            WriteLog("获取公网地址……");

            foreach (var item in Servers)
            {
                if (item.Local.IsTcp) continue;

                // 查询公网地址和网络类型，如果是受限网络，或者对称网络，则采用本地端口，因此此时只能依赖端口映射，将来这里可以考虑操作UPnP
                if (rs == null)
                {
                    item.Start();
                    rs = new StunClient(item).Query();
                    if (rs != null)
                    {
                        if (rs != null && rs.Type == StunNetType.Blocked && rs.Public != null) rs.Type = StunNetType.Symmetric;
                        WriteLog("网络类型：{0} {1}", rs.Type, rs.Type.GetDescription());
                        ep = rs.Public;
                        if (ep != null) pub = ep.Address;
                    }
                }
                else
                    ep = new StunClient(item).GetPublic();
                if (rs != null && rs.Type > StunNetType.AddressRestrictedCone) ep = new IPEndPoint(pub, item.Port);
                WriteLog("{0}的公网地址：{1}", item.Local, ep);
                dic.Add(item.Port, ep);
            }
            // Tcp没办法获取公网地址，只能通过Udp获取到的公网地址加上端口形成，所以如果要使用Tcp，服务器必须拥有独立公网地址
            foreach (var item in Servers)
            {
                if (item.Local.IsTcp)
                {
                    ep = new IPEndPoint(pub, item.Port);
                    WriteLog("{0}的公网地址：{1}", item.Local, ep);
                    dic.Add(item.Port + 100000, ep);
                }
            }
            //var ep = StunClient.GetPublic(Port, 2000);
            //WriteLog("端口{0}的公网地址：{1}", Port, ep);
            //dic.Add(Port, ep);
            //ep = StunClient.GetPublic(Port2, 2000);
            //WriteLog("端口{0}的公网地址：{1}", Port2, ep);
            //dic.Add(Port2, ep);
            WriteLog("成功获取公网地址！");
            Public = dic;
        }
    }
    #endregion

    #region 方法
    /// <summary>接收到数据时</summary>
    /// <param name="session"></param>
    /// <param name="pk"></param>
    protected override void OnReceive(INetSession session, IPacket pk)
    {
        if (pk.Total > 0)
        {
            var remote = session.Remote;
            //if (remote == null && session != null) remote = session.RemoteEndPoint;

            var request = StunMessage.Read(pk.GetStream());
            WriteLog("{0} {1} {2}{3}", request.Type, remote, request.ChangeIP ? " ChangeIP" : "", request.ChangePort ? " ChangePort" : "");

            // 如果是兄弟服务器发过来的，修正响应地址
            switch (request.Type)
            {
                case StunMessageType.BindingRequest:
                    //case StunMessageType.BindingResponse:
                    request.Type = StunMessageType.BindingRequest;
                    if (request.ResponseAddress != null) remote.EndPoint = request.ResponseAddress;
                    break;
                case StunMessageType.SharedSecretRequest:
                    //case StunMessageType.SharedSecretResponse:
                    request.Type = StunMessageType.SharedSecretRequest;
                    if (request.ResponseAddress != null) remote.EndPoint = request.ResponseAddress;
                    break;
                default:
                    break;
            }

            // 是否需要发给伙伴
            if (request.ChangeIP)
            {
                //if (Partner != null && !Partner.Equals(session.Host.LocalEndPoint.GetRelativeEndPoint(Partner.Address)))
                //{
                //    // 发给伙伴
                //    request.ChangeIP = false;
                //    // 记住对方的地址
                //    request.ResponseAddress = remote.EndPoint;
                //    //session.Send(request.GetStream(), Partner);
                //    var us = session.Host as UdpServer;
                //    if (us != null)
                //    {
                //        //us.CreateSession(Partner).Send(request.GetStream());
                //        us.Send(request.GetStream(), Partner);
                //    }
                //    return;
                //}
                // 如果没有伙伴地址，采用不同端口代替
                request.ChangePort = true;
            }

            // 开始分流处理
            switch (request.Type)
            {
                case StunMessageType.BindingRequest:
                    //case StunMessageType.BindingResponse:
                    OnBind(request, session.Session);
                    break;
                case StunMessageType.SharedSecretRequest:
                    break;
                default:
                    break;
            }
        }
    }
    #endregion

    #region 绑定
    /// <summary>绑定</summary>
    /// <param name="request"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    protected void OnBind(StunMessage request, ISocketSession session)
    {
        var rs = new StunMessage
        {
            Type = StunMessageType.BindingResponse,
            TransactionID = request.TransactionID.ReadBytes(0, -1),
            MappedAddress = session.Remote.EndPoint
        };
        //rs.SourceAddress = session.GetRelativeEndPoint(remote.Address);
        if (Public != null)
        {
            if (session.Local.IsTcp)
                rs.SourceAddress = Public[session.Port + 100000];
            else
                rs.SourceAddress = Public[session.Port];
        }

        // 找另一个
        ISocketSession session2 = null;
        var anotherPort = 0;
        for (var i = 0; i < Servers.Count; i++)
        {
            var server = Servers[i];
            if (server.Local.Type == session.Local.Type && server.Local.Port != session.Local.Port)
            {
                anotherPort = server.Port;
                if (server.Local.IsTcp)
                {
                    break;
                }
                else
                {
                    session2 = (server as UdpServer).CreateSession(IPAddress.Any, session.Remote.EndPoint);
                    if (session2 != null) break;
                }
            }
        }
        //rs.ChangedAddress = Partner ?? session2.GetRelativeEndPoint(remote.Address);
        if (Public != null)
        {
            if (session.Local.IsTcp)
                rs.ChangedAddress = Partner ?? Public[anotherPort + 100000];
            else
                rs.ChangedAddress = Partner ?? Public[anotherPort];
        }

        var name = Name;
        if (name == GetType().Name) name = GetType().FullName;
        rs.ServerName = String.Format("{0} v{1}", name, AssemblyX.Create(Assembly.GetExecutingAssembly()).Version);

        // 换成另一个
        if (request.ChangePort) session = session2;

        session.Send(rs.ToArray());
    }
    #endregion

    #region 辅助
    static Boolean IsResponse(StunMessageType type)
    {
        return ((UInt16)type & 0x0100) != 0;
    }
    #endregion
}