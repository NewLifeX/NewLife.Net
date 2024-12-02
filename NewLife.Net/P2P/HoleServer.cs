﻿using NewLife.Data;

namespace NewLife.Net.P2P;

/// <summary>打洞服务器</summary>
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
/// </remarks>
public class HoleServer : NetServer
{
    #region 属性
    private Dictionary<String, INetSession> _Clients;
    /// <summary>客户端集合</summary>
    public IDictionary<String, INetSession> Clients { get { return _Clients ?? (_Clients = new Dictionary<String, INetSession>(StringComparer.OrdinalIgnoreCase)); } }
    #endregion

    #region 方法
    /// <summary>收到数据时</summary>
    /// <param name="session"></param>
    /// <param name="pk"></param>
    protected override void OnReceive(INetSession session, IPacket pk)
    {
        var remoteEP = session.Remote.EndPoint;

        var str = pk.ToStr();
        WriteLog("");
        WriteLog(remoteEP + "=>" + session.Session.Local.EndPoint + " " + str);

        var ss = str.Split(":");
        ss[0] = ss[0].ToLower();
        if (ss[0] == "reg")
        {
            var name = ss[1];
            if (!Clients.TryGetValue(name, out var ns))
            {
                // 集合里面没有，认为是发起邀请方，做好记录
                ns = new NetSession
                {
                    //ns.Server = sender as ISocketServer;
                    Session = session.Session
                };
                //ns.ClientEndPoint = remoteEP;
                Clients[name] = ns;
                session.OnDisposed += (s, e2) => ns.Dispose();
                ns.OnDisposed += (s, e2) => Clients.Remove(name);

                //session.Send("注册成功！你的公网地址是：" + client, null, client);
                session.Send("注册成功！你的公网地址是：" + remoteEP, null);

                WriteLog("邀请已建立：{0}", name);
            }
            else
            {
                // 如果连续注册两次可能会有问题，这里要处理一下
                //if ("" + ns.ClientEndPoint == "" + remoteEP)
                //    session.Send("Has Register!", null);
                //else
                {
                    // 到这里，应该是被邀请方到来，同时响应双方
                    session.Send(remoteEP.ToString(), null);

                    // 同时还要通知对方
                    ns.Session.Send(remoteEP.ToString(), null);

                    WriteLog("邀请已接受：{0} {1} {2}", name, remoteEP, remoteEP);

                    Clients.Remove(name);
                    Thread.Sleep(1000);
                    //session.Disconnect();
                    //if (ns.Session != null) ns.Session.Disconnect();
                    session.Dispose();
                    ns.Session.Dispose();
                }
            }
        }
        else if (ss[0] == "invite")
        {
            if (Clients.TryGetValue(ss[1], out var ns))
            {
                session.Send("invite:" + remoteEP, null);

                // 同时还要通知对方
                ns.Session.Send("invite:" + remoteEP.ToString(), null);
            }
            else
                session.Send("Not Found!", null);
        }
        else
        {
            if (!str.Contains("P2P")) session.Send("无法处理的信息：" + str, null);
        }
    }
    #endregion
}