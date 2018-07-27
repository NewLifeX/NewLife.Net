using System;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    /// <remarks>
    /// 网络代理分为本地服务器、客户端、远程服务器三种角色，本地服务器负责监听并转发客户端和远程服务器之间的所有数据。
    /// </remarks>
    public abstract class ProxyBase : NetServer
    {
        #region 属性
        /// <summary>开始会话时连接远程会话。默认true</summary>
        public Boolean ConnectRemoteOnStart { get; set; } = true;
        #endregion

        #region 构造函数
        /// <summary></summary>
        public ProxyBase()
        {
            //必须要使UseSession = true，否则创建的session对象无Host属性，在ShowSession时，无法获取Host.Name
            UseSession = true;
        }
        #endregion

        #region 业务
        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(ISocketSession session) => new ProxySession { Host = this };

        /// <summary>添加会话</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            if (session is ProxySession ss) ss.Host = this;

            base.AddSession(session);
        }
        #endregion
    }
}