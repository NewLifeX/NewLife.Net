using System;
using NewLife;
using NewLife.Net;

namespace EchoTest
{
    /// <summary>定义服务端，用于管理所有网络会话</summary>
    class MyNetServer : NetServer<MyNetSession>
    {
    }

    /// <summary>定义会话。每一个远程连接唯一对应一个网络会话，再次重复收发信息</summary>
    class MyNetSession : NetSession<MyNetServer>
    {
        /// <summary>客户端连接</summary>
        public override void Start()
        {
            base.Start();

#if DEBUG
            // 欢迎语
            var str = String.Format("Welcome to visit {1}!  [{0}]\r\n", Remote, Environment.MachineName);
            Send(str);
#endif
        }

        /// <summary>收到客户端数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
#if DEBUG
            WriteLog("收到：{0}", e.Packet.ToStr());
#endif

            // 把收到的数据发回去
            Send(e.Packet);
        }

        /// <summary>断开</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
#if DEBUG
            WriteLog("断开：{0}", Remote);
#endif

            base.Dispose(disposing);
        }

        /// <summary>出错</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnError(Object sender, ExceptionEventArgs e)
        {
#if DEBUG
            WriteLog("[{0}]错误：{1}", e.Action, e.Exception?.GetTrue().Message);
#endif

            base.OnError(sender, e);
        }
    }
}