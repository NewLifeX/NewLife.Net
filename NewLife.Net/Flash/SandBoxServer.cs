﻿using NewLife.Data;

namespace NewLife.Net;

/// <summary>安全沙箱</summary>
public class SandBoxServer : NetServer
{
    #region 属性
    private String _Policy = "<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"*\" /></cross-domain-policy>\0";
    /// <summary>安全策略文件内容</summary>
    public String Policy { get { return _Policy; } set { _Policy = value; } }
    #endregion

    /// <summary>实例化一个安全沙箱服务器</summary>
    public SandBoxServer()
    {
        Port = 843;
        ProtocolType = NetType.Tcp;
    }
    /// <summary>数据返回</summary>
    /// <param name="session"></param>
    /// <param name="pk"></param>
    protected override void OnReceive(INetSession session, IPacket pk)
    {
        var sss = pk.ToStr();
        if (sss == "<policy-file-request/>\0")
        {
            session.Send(System.Text.Encoding.UTF8.GetBytes(_Policy.ToCharArray()));
        }
        session.Dispose();
        return;
    }
}
