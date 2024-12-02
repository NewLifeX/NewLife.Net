﻿using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net.Application;

/// <summary>日志服务</summary>
public class LogServer : NetServer<LogServer.LogSession>
{
    /// <summary>实例化一个日志服务</summary>
    public LogServer()
    {
        // 默认514端口
        Port = 514;

        Name = "日志服务";
    }

    /// <summary>日志会话</summary>
    public class LogSession : NetSession
    {
        /// <summary>实例化</summary>
        public LogSession()
        {
            // 不需要日志前缀
            //LogPrefix = "";
        }

        /// <summary>开始会话</summary>
        public override void Start()
        {
            Log = XTrace.Log;

            LogPrefix = $"[{ID}]";

            base.Start();
        }

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Packet.Total == 0) return;

            WriteLog("{0}", e.Packet.ToStr());
        }
    }
}