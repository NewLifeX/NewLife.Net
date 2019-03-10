using NewLife.Data;

namespace NewLife.Net.Application
{
    /// <summary>Echo服务。把客户端发来的数据原样返回。</summary>
    public class EchoServer : NetServer
    {
        /// <summary>实例化一个Echo服务</summary>
        public EchoServer()
        {
            // 默认7端口
            Port = 7;

            Name = "Echo服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="session"></param>
        /// <param name="pk"></param>
        protected override void OnReceive(INetSession session, Packet pk)
        {
            var count = pk.Total;
            if (count == 0) return;

            //var p = pk.Position;
            if (count > 100)
                WriteLog("Echo {0} [{1}]", session.Remote, count);
            else
                WriteLog("Echo {0} [{1}] {2}", session.Remote, count, pk.ToStr());

            //Send(e.Socket, e.Buffer, e.Offset, stream.Length, e.RemoteEndPoint);
            //session.Send(e.Buffer, e.Offset, stream.Length, e.RemoteEndPoint);
            //session.Send(e.Buffer, e.Offset, stream.Length);
            //pk.Position = p;
            //session.Send(stream);
            session.Send(pk);
        }
    }
}