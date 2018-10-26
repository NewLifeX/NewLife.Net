using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Agent;
using NewLife.Log;
using NewLife.Net;

namespace EchoAgent
{
    class Program
    {
        static void Main(String[] args)
        {
            // 引导进入我的服务控制类
            MyService.ServiceMain();
        }
    }

    class MyService : AgentServiceBase<MyService>
    {
        public MyService()
        {
            ServiceName = "EchoAgent";
            DisplayName = "回声服务";
            Description = "这是NewLife.Net的一个回声服务示例！";

            // 准备两个工作线程，分别负责输出日志和向客户端发送时间
            //ThreadCount = 2;
            ThreadCount = 1;
            Intervals = new[] { 1, 5 };
        }

        MyNetServer _Server;
        /// <summary>开始服务</summary>
        /// <param name="reason"></param>
        protected override void StartWork(String reason)
        {
            // 实例化服务端，指定端口，同时在Tcp/Udp/IPv4/IPv6上监听
            var svr = new MyNetServer
            {
                Port = 1234,
                Log = XTrace.Log
            };
            svr.Start();

            _Server = svr;

            base.StartWork(reason);
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected override void StopWork(String reason)
        {
            _Server.TryDispose();
            _Server = null;

            base.StopWork(reason);
        }

        /// <summary>调度器让每个任务线程定时执行Work，index标识任务</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Boolean Work(Int32 index)
        {
            switch (index)
            {
                case 0: ShowStat(_Server); break;
                case 1: SendTime(_Server); break;
            }
            return false;
        }

        private String _last;
        /// <summary>显示服务端状态</summary>
        /// <param name="ns"></param>
        private void ShowStat(NetServer ns)
        {
            var msg = ns.GetStat();
            if (msg == _last) return;

            _last = msg;

            WriteLog(msg);
        }

        /// <summary>向所有客户端发送时间</summary>
        /// <param name="ns"></param>
        private void SendTime(NetServer ns)
        {
            var str = DateTime.Now.ToFullString() + Environment.NewLine;
            var buf = str.GetBytes();
            ns.SendAllAsync(buf);
        }
    }
}