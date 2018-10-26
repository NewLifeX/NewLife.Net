using System;
using NewLife.Data;

namespace RpcTest
{
    /// <summary>自定义控制器。包含多个服务</summary>
    class MyController
    {
        /// <summary>添加，标准业务服务，走Json序列化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Int32 Add(Int32 x, Int32 y) => x + y;

        /// <summary>RC4加解密，高速业务服务，二进制收发不经序列化</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Packet RC4(Packet pk)
        {
            var data = pk.ToArray();
            var pass = "NewLife".GetBytes();

            return data.RC4(pass);
        }
    }
}