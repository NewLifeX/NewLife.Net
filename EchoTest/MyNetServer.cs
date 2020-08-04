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
        protected override void OnConnected()
        {
            // 发送欢迎语
            Send($"Welcome to visit {Environment.MachineName}!  [{Remote}]\r\n");

            base.OnConnected();
        }

        /// <summary>客户端断开连接</summary>
        protected override void OnDisconnected()
        {
#if DEBUG
            WriteLog("客户端{0}已经断开连接啦", Remote);
#endif

            base.OnDisconnected();
        }

        /// <summary>收到客户端数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
#if DEBUG
            WriteLog("收到：{0}", e.Packet.ToStr());
#endif

            //todo 这里是业务处理核心，解开数据包e.Packet并进行业务处理

            // 把收到的数据发回去
            Send(e.Packet);
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