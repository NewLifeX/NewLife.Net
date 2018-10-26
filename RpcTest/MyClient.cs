using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Remoting;

namespace RpcTest
{
    /// <summary>自定义业务客户端</summary>
    class MyClient : ApiClient
    {
        public MyClient(String uri) : base(uri) { }

        /// <summary>添加，标准业务服务，走Json序列化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public async Task<Int32> AddAsync(Int32 x, Int32 y)
        {
            return await InvokeAsync<Int32>("My/Add", new { x, y });
        }

        /// <summary>RC4加解密，高速业务服务，二进制收发不经序列化</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public async Task<Packet> RC4Async(Packet pk)
        {
            return await InvokeAsync<Packet>("My/RC4", pk);
        }

        public async Task<User> FindUserAsync(Int32 uid, Boolean enable)
        {
            return await InvokeAsync<User>("User/FindByID", new { uid, enable });
        }
    }
}