using System;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net.Application
{
    /// <summary>回声处理器</summary>
    public class EchoHandler : Handler
    {
        /// <summary>读取</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            var ctx = context as NetHandlerContext;
            var session = ctx.Session;

            if (message is Packet pk)
            {
                var len = pk.Total;
                if (len > 100)
                    XTrace.WriteLine("Echo {0} [{1}]", session, len);
                else
                    XTrace.WriteLine("Echo {0} [{1}] {2}", session, len, pk.ToStr());
            }
            else
                XTrace.WriteLine("{0}", message);

            session.SendMessage(message);

            return null;
        }
    }
}