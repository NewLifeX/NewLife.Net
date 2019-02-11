using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Net.DICOM
{
    /// <summary>DICOM会话。每一个远程连接唯一对应一个网络会话，再次重复收发信息</summary>
    public class DicomSession : NetSession<DicomServer>
    {
        ///// <summary>客户端连接</summary>
        //public override void Start()
        //{
        //    base.Start();

        //    // 欢迎语
        //    var str = String.Format("Welcome to visit {1}!  [{0}]\r\n", Remote, Environment.MachineName);
        //    Send(str);
        //}

        /// <summary>收到客户端数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //WriteLog("收到：{0}", e.Packet.ToStr());

            // 把收到的数据发回去
            Send(e.Packet);
        }
    }
}