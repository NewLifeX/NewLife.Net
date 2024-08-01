using System;
using NewLife;
using NewLife.Agent;
using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;

namespace EchoAgent
{
    class Program
    {
        static void Main(String[] args) => new MyService().Main(args);
    }

    class MyService : ServiceBase
    {
        public MyService()
        {
            ServiceName = "EchoAgent";
            DisplayName = "回声服务";
            Description = "这是NewLife.Net的一个回声服务示例！";
        }

        MyNetServer _Server;
        /// <summary>开始服务</summary>
        /// <param name="reason"></param>
        public override void StartWork(String reason)
        {
            // 实例化服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
            var svr = new MyNetServer
            {
                Port = 1234,
                Log = XTrace.Log,
                Tracer = new DefaultTracer { Period = 15, Log = XTrace.Log },
#if DEBUG
                SocketLog = XTrace.Log,
                LogSend = true,
                LogReceive = true,
#endif
            };
            svr.Start();

            _Server = svr;

            _timer1 = new TimerX(s => ShowStat(_Server), null, 1000, 1000) { Async = true };
            _timer2 = new TimerX(s => SendTime(_Server), null, 1000, 1000) { Async = true };

            base.StartWork(reason);
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        public override void StopWork(String reason)
        {
            _Server.TryDispose();
            _Server = null;

            base.StopWork(reason);
        }

        private TimerX _timer1;
        private TimerX _timer2;

        private String _last;
        /// <summary>显示服务端状态</summary>
        /// <param name="ns"></param>
        private void ShowStat(NetServer ns)
        {
            if (ns == null) return;

            var msg = ns.GetStat();
            if (msg == _last) return;

            _last = msg;

            WriteLog(msg);
        }

        /// <summary>向所有客户端发送时间</summary>
        /// <param name="ns"></param>
        private void SendTime(NetServer ns)
        {
            if (ns == null) return;

            var str = DateTime.Now.ToFullString() + Environment.NewLine;
            var buf = str.GetBytes();
            ns.SendAllAsync(buf);
        }
    }
}