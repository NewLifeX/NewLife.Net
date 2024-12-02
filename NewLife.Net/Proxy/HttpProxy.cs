﻿using System.Net;
using NewLife.Data;
using NewLife.Net.Http;

namespace NewLife.Net.Proxy;

/// <summary>Http代理。可用于代理各种Http通讯请求。</summary>
/// <remarks>Http代理请求与普通请求唯一的不同就是Uri，Http代理请求收到的是可能包括主机名的完整Uri</remarks>
public class HttpProxy : ProxyBase
{
    #region 构造
    /// <summary>实例化</summary>
    public HttpProxy()
    {
        Port = 8080;
        ProtocolType = NetType.Tcp;

        Cache = new HttpCache();
    }
    #endregion

    #region 事件
    /// <summary>收到请求时发生。</summary>
    public event EventHandler<HttpProxyEventArgs> OnRequest;

    /// <summary>收到响应时发生。</summary>
    public event EventHandler<HttpProxyEventArgs> OnResponse;

    /// <summary>收到请求主体时发生。</summary>
    public event EventHandler<HttpProxyEventArgs> OnRequestBody;

    /// <summary>收到响应主体时发生。</summary>
    public event EventHandler<HttpProxyEventArgs> OnResponseBody;

    /// <summary>触发事件</summary>
    /// <param name="session"></param>
    /// <param name="kind"></param>
    /// <param name="he"></param>
    /// <returns>返回是否取消操作</returns>
    Boolean RaiseEvent(Session session, EventKind kind, HttpProxyEventArgs he)
    {
        GetHandler(kind)?.Invoke(session, he);

        return he.Cancel;
    }

    EventHandler<HttpProxyEventArgs> GetHandler(EventKind kind)
    {
        switch (kind)
        {
            case EventKind.OnRequest:
                return OnRequest;
            case EventKind.OnResponse:
                return OnResponse;
            case EventKind.OnRequestBody:
                return OnRequestBody;
            case EventKind.OnResponseBody:
                return OnResponseBody;
            default:
                break;
        }
        return null;
    }

    enum EventKind
    {
        OnRequest,
        OnResponse,
        OnRequestBody,
        OnResponseBody
    }
    #endregion

    #region 缓存
    /// <summary>激活缓存</summary>
    public Boolean EnableCache { get; set; }

    /// <summary>Http缓存</summary>
    HttpCache Cache { get; set; }
    #endregion

    #region 会话
    /// <summary>创建会话</summary>
    /// <param name="session"></param>
    /// <returns></returns>
    protected override INetSession CreateSession(ISocketSession session) => new Session();

    /// <summary>Http反向代理会话</summary>
    public class Session : ProxySession
    {
        /// <summary>当前正在处理的请求。一个连接同时只能处理一个请求，除非是Http 1.2</summary>
        HttpHeader UnFinishedRequest;

        /// <summary>已完成处理，正在转发数据的请求头</summary>
        public HttpHeader Request;

        /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
        /// <remarks>
        /// 如果数据包包括头部和主体，可以分开处理。
        /// 最麻烦的就是数据包不是一个完整的头部，还落了一部分在后面的包上。
        /// </remarks>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Packet.Total == 0)
            {
                base.OnReceive(e);
                return;
            }

            var pxy = Host as HttpProxy;
            #region 解析请求头
            // 解析请求头。
            var stream = e.Packet.GetStream() as Stream;
            // 当前正在处理的未完整的头部，浏览器可能把请求头分成几块发过来
            var entity = UnFinishedRequest;
            // 如果当前请求为空，说明这是第一个数据包，可能包含头部
            if (entity == null)
            {
                // 读取并分析头部
                entity = HttpHeader.Read(stream, HttpHeaderReadMode.Request);
                if (entity == null)
                {
                    // 分析失败？这个可能不是Http请求头
                    var he = new HttpProxyEventArgs(Request, stream);
                    if (pxy.RaiseEvent(this, EventKind.OnRequestBody, he)) return;
                    //e.Stream = he.Stream;
                    e.Packet = (ArrayPacket)he.Stream.ReadBytes(-1);

                    base.OnReceive(e);

                    return;
                }

                // 根据完成情况保存到不同的本地变量中
                if (!entity.IsFinish)
                    UnFinishedRequest = entity;
                else
                    Request = entity;
            }
            else if (!entity.IsFinish)
            {
                // 如果请求未完成，说明现在的数据内容还是头部
                entity.ReadHeaders(stream);
                if (entity.IsFinish)
                {
                    Request = entity;
                    UnFinishedRequest = null;
                }
            }
            else
            {
                // 否则，头部已完成，现在就是内容，直接转发
                var he = new HttpProxyEventArgs(Request, stream);
                if (pxy.RaiseEvent(this, EventKind.OnRequestBody, he)) return;
                //e.Stream = he.Stream;
                e.Packet = (ArrayPacket)he.Stream.ReadBytes(-1);

                base.OnReceive(e);

                return;
            }

            // 请求头不完整，不发送，等下一部分到来
            if (!entity.IsFinish) return;
            #endregion

            WriteLog("{0}", entity.Url);

            #region 重构请求包
            // 现在所在位置是一个全新的请求
            var rs = OnRequest(entity, e);
            {
                var he = new HttpProxyEventArgs(Request, stream)
                {
                    Cancel = !rs
                };
                rs = !pxy.RaiseEvent(this, EventKind.OnRequest, he);
            }
            if (!rs) return;

            // 如果流中还有数据，可能是请求体，也要拷贝
            if (stream.Position < stream.Length)
            {
                var he = new HttpProxyEventArgs(Request, stream);
                if (pxy.RaiseEvent(this, EventKind.OnRequestBody, he)) return;
                stream = he.Stream;
            }

            // 重新构造请求
            var ms = new MemoryStream();
            entity.Write(ms);
            stream.CopyTo(ms);
            ms.Position = 0;

            //e.Stream = ms;
            e.Packet = (ArrayPacket)ms.ToArray();
            #endregion

            base.OnReceive(e);
        }

        /// <summary>是否保持连接</summary>
        Boolean KeepAlive = false;
        DateTime RequestTime;

        /// <summary>收到请求时</summary>
        /// <param name="entity"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual Boolean OnRequest(HttpHeader entity, ReceivedEventArgs e)
        {
            var host = "";
            var oriUrl = entity.Url;

            // 特殊处理CONNECT
            if (entity.Method.EqualIgnoreCase("CONNECT")) return ProcessConnect(entity, e);

            // 检查缓存
            if (GetCache(entity, e)) return false;

            var remote = RemoteServer;
            var ruri = RemoteServerUri;
            if (entity.Url.IsAbsoluteUri)
            {
                var uri = entity.Url;
                host = uri.Host + ":" + uri.Port;

                // 如果地址或端口改变，则重新连接服务器
                if (remote != null && (uri.Host != ruri.Host || uri.Port != ruri.Port))
                {
                    remote.Dispose();
                    RemoteServer = null;
                }
                //RemoteHost = uri.Host;
                //LastPort = uri.Port;

                //RemoteEndPoint = new IPEndPoint(NetHelper.ParseAddress(uri.Host), uri.Port);
                ruri.Host = uri.Host;
                ruri.Port = uri.Port;
                entity.Url = new Uri(uri.PathAndQuery, UriKind.Relative);
            }
            else if (!String.IsNullOrEmpty(entity.Host))
            {
                //RemoteEndPoint = NetHelper.ParseEndPoint(entity.Host, 80);
                ruri.Host = entity.Host;
                ruri.Port = 80;
            }
            else
                throw new XException("无法处理的请求！{0}", entity);

            //WriteDebugLog("[{4}] {3} => {0} {1} [{2}]", entity.Method, oriUrl, entity.ContentLength, Session.Remote.EndPoint, ID);

            // 可能不含Host
            if (String.IsNullOrEmpty(entity.Host)) entity.Host = host;

            // 处理KeepAlive
            KeepAlive = false;
            if (!String.IsNullOrEmpty(entity.ProxyConnection))
            {
                entity.KeepAlive = entity.ProxyKeepAlive;
                entity.ProxyConnection = null;

                KeepAlive = entity.ProxyKeepAlive;
            }

            RequestTime = DateTime.Now;

            return true;
        }

        Boolean ProcessConnect(HttpHeader entity, ReceivedEventArgs e)
        {
            //WriteDebugLog("[{3}] {0} {1} [{2}]", entity.Method, entity.Url, entity.ContentLength, ID);
            var pxy = Host as HttpProxy;

            //var host = entity.Url.ToString();
            var uri = RemoteServerUri;
            var ep = NetHelper.ParseEndPoint(entity.Url.ToString(), 80);
            uri.EndPoint = ep;

            // 不要连自己，避免死循环
            if (ep.Port == pxy.Server.Port &&
                (ep.Address == IPAddress.Loopback || ep.Address == IPAddress.IPv6Loopback))
            {
                WriteLog("不要连自己，避免死循环");
                Dispose();
                return false;
            }

            var rs = new HttpHeader
            {
                Version = entity.Version
            };
            try
            {
                // 连接远程服务器，启动数据交换
                if (RemoteServer == null) StartRemote(e);

                rs.StatusCode = 200;
                rs.StatusDescription = "OK";
            }
            catch (Exception ex)
            {
                rs.StatusCode = 500;
                rs.StatusDescription = ex.Message;
            }

            // 告诉客户端，已经连上了服务端，或者没有连上，这里不需要向服务端发送任何数据
            Send(rs.GetStream());
            return false;
        }

        /// <summary>检查是否存在缓存，如果存在，则直接返回缓存</summary>
        /// <param name="entity"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual Boolean GetCache(HttpHeader entity, ReceivedEventArgs e)
        {
            var pxy = Host as HttpProxy;
            if (!pxy.EnableCache || entity == null) return false;

            if (entity.Method.EqualIgnoreCase("GET"))
            {
                // 查找缓存
                var citem = pxy.Cache.GetItem(entity.RawUrl);
                if (citem != null)
                {
                    // 响应缓存
                    Byte[] cs = null;
                    var ms = citem.Stream;
                    if (ms is MemoryStream)
                        cs = (ms as MemoryStream).ToArray();
                    else
                    {
                        lock (ms)
                        {
                            ms.Position = 0;
                            cs = ms.ReadBytes(-1);
                        }
                    }
                    //var cs = citem.Response.GetStream();

                    //WriteDebugLog("[{0}] {1} 缓存命中[{2}]", ID, entity.RawUrl, cs.Length);

                    Send(cs);

                    return true;
                }
            }

            return false;
        }

        HttpHeader UnFinishedResponse;
        HttpHeader Response;

        HttpCacheItem cacheItem;

        /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
        /// <param name="e"></param>
        /// <returns>修改后的数据</returns>
        protected override void OnReceiveRemote(ReceivedEventArgs e)
        {
            var pxy = Host as HttpProxy;
            var parseHeader = pxy.EnableCache || pxy.GetHandler(EventKind.OnResponse) != null;
            var parseBody = pxy.EnableCache || pxy.GetHandler(EventKind.OnResponseBody) != null;

            var entity = UnFinishedResponse;
            var stream = e.Packet.GetStream() as Stream;
            if (parseHeader || parseBody)
            {
                #region 解析响应头
                // 解析头部
                if (entity == null)
                {
                    #region 未完成响应为空，可能是第一个响应包，也可能是后续数据包
                    // 如果当前未完成响应为空，说明这是第一个数据包，可能包含头部
                    entity = HttpHeader.Read(stream, HttpHeaderReadMode.Response);
                    if (entity == null)
                    {
                        var he = new HttpProxyEventArgs(Response, stream);
                        if (pxy.RaiseEvent(this, EventKind.OnResponseBody, he)) return;
                        //e.Stream = he.Stream;
                        e.Packet = (ArrayPacket)he.Stream.ReadBytes(-1);

                        // 如果现在正在缓存之中，那么也罢这些非头部数据一并拷贝到缓存里面
                        if (cacheItem != null)
                        {
                            var ms = e.Packet.GetStream();
                            var p = ms.Position;
                            ms.CopyTo(cacheItem.Stream);
                            var count = ms.Position = p;
                            ms.Position = p;
                            //WriteDebugLog("[{0}] {1} 缓存数据[{2}]", ID, Request.RawUrl, count);
                        }

                        base.OnReceiveRemote(e);
                        return;
                    }

                    if (!entity.IsFinish)
                        UnFinishedResponse = entity;
                    else
                        Response = entity;
                    #endregion
                }
                else if (!entity.IsFinish)
                {
                    #region 未完成响应，继续读取头部
                    // 如果请求未完成，说明现在的数据内容还是头部
                    entity.ReadHeaders(stream);
                    if (entity.IsFinish)
                    {
                        Response = entity;
                        UnFinishedResponse = null;
                    }
                    #endregion
                }
                else
                {
                    #region 未完成响应的头部已完成？似乎不大可能
                    // 否则，头部已完成，现在就是内容
                    var he = new HttpProxyEventArgs(Response, stream);
                    if (pxy.RaiseEvent(this, EventKind.OnResponseBody, he)) return;
                    base.OnReceiveRemote(e);
                    //e.Stream = he.Stream;
                    e.Packet = (ArrayPacket)he.Stream.ReadBytes(-1);

                    // 如果现在正在缓存之中，那么也罢这些非头部数据一并拷贝到缓存里面
                    if (cacheItem != null)
                    {
                        var ms = e.Packet.GetStream();
                        var p = ms.Position;
                        ms.CopyTo(cacheItem.Stream);
                        var count = ms.Position = p;
                        ms.Position = p;
                        //WriteDebugLog("[{0}] {1} 缓存数据[{2}]", ID, Request.RawUrl, count);
                    }

                    return;
                    #endregion
                }
                #endregion

                // 请求头不完整，不发送，等下一部分到来
                if (!entity.IsFinish) return;

                {
                    var he = new HttpProxyEventArgs(entity, stream);
                    if (pxy.RaiseEvent(this, EventKind.OnResponse, he)) return;
                    stream = he.Stream;
                }

                // 写入头部扩展
                entity.Headers["Powered-By-Proxy"] = pxy.Name;
                entity.Headers["RequestTime"] = RequestTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                entity.Headers["TotalTime"] = (DateTime.Now - RequestTime).ToString();
            }

            #region 缓存
            if (pxy.EnableCache) SetCache(Response, e);
            #endregion

            #region 重构响应包
            if (entity != null)
            {
                var ms = new MemoryStream();
                entity.Write(ms);

                if (parseBody && stream.Position < stream.Length)
                {
                    var he = new HttpProxyEventArgs(Response, stream);
                    if (pxy.RaiseEvent(this, EventKind.OnResponseBody, he)) return;
                    stream = he.Stream;
                }

                stream.CopyTo(ms);
                ms.Position = 0;

                //stream = ms;
                //e.Stream = ms;
                e.Packet = (ArrayPacket)ms.ToArray();
            }

            if (cacheItem != null)
            {
                var ms = e.Packet.GetStream();
                var p = ms.Position;
                ms.CopyTo(cacheItem.Stream);
                var count = ms.Position = p;
                ms.Position = p;
                //WriteDebugLog("[{0}] {1} 增加缓存[{2}]", ID, Request.RawUrl, count);
            }
            #endregion

            base.OnReceiveRemote(e);
        }

        static readonly HashSet<String> cacheSuffix = new HashSet<String>(
            new String[] { ".htm", ".html", ".js", ".css", ".jpg", ".png", ".gif", ".swf" },
            StringComparer.OrdinalIgnoreCase);

        static readonly HashSet<String> cacheContentType = new HashSet<String>(
            new String[] { "text/css", "application/javascript", "text/javascript", "application/x-javascript", "image/jpeg", "image/png", "image/gif" },
            StringComparer.OrdinalIgnoreCase);

        /// <summary>如果符合缓存条件，则设置缓存</summary>
        /// <param name="entity"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual Boolean SetCache(HttpHeader entity, ReceivedEventArgs e)
        {
            // 既然是新响应，那么需要首先重置缓存项
            cacheItem = null;

            var request = Request;
            var response = entity;
            if (request == null || response == null) return false;

            var pxy = Host as HttpProxy;
            var cache = pxy.Cache;
            if (request.Method.EqualIgnoreCase("GET"))
            {
                if (response.StatusCode == 304)
                {
                    response.Headers["HttpProxyCache"] = "304";
                    cacheItem = cache.Add(request, response);
                    return true;
                }

                var url = request.RawUrl;
                if (!String.IsNullOrEmpty(url) && url[url.Length - 1] != '/' && cacheSuffix.Contains(Path.GetExtension(url)))
                {
                    response.Headers["HttpProxyCache"] = Path.GetExtension(url);
                    cacheItem = cache.Add(request, response);
                    return true;
                }

                var contentType = response.ContentType;
                if (!String.IsNullOrEmpty(contentType))
                {
                    var p = contentType.IndexOf(";");
                    if (p < 0 || cacheContentType.Contains(contentType.Substring(0, p)))
                    {
                        response.Headers["HttpProxyCache"] = contentType;
                        cacheItem = cache.Add(request, response);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>远程连接断开时触发。默认销毁整个会话，子类可根据业务情况决定客户端与代理的链接是否重用。</summary>
        /// <param name="session"></param>
        protected override void OnRemoteDispose(ISocketClient session)
        {
            // 如果客户端不要求保持连接，就销毁吧
            if (!KeepAlive) base.OnRemoteDispose(session);
        }
    }
    #endregion

    #region 浏览器代理
    //struct Struct_INTERNET_PROXY_INFO
    //{
    //    public Int32 dwAccessType;
    //    public IntPtr proxy;
    //    public IntPtr proxyBypass;
    //}

    ///// <summary>定义API函数</summary>
    ///// <param name="hInternet"></param>
    ///// <param name="dwOption"></param>
    ///// <param name="lpBuffer"></param>
    ///// <param name="lpdwBufferLength"></param>
    ///// <returns></returns>
    //[DllImport("wininet.dll", SetLastError = true)]
    //private static extern Boolean InternetSetOption(IntPtr hInternet, Int32 dwOption, IntPtr lpBuffer, Int32 lpdwBufferLength);

    ///// <summary>获取IE代理设置</summary>
    //public static String GetIEProxy()
    //{
    //    using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
    //    {
    //        if (key == null) return null;

    //        var obj = key.GetValue("ProxyEnable", 0);
    //        if (obj == null || (Int32)obj == 0) return null;

    //        obj = key.GetValue("ProxyServer");
    //        return obj == null ? null : "" + obj;
    //    }
    //}

    ///// <summary>设置IE代理。传入空地址取消代理设置</summary>
    ///// <param name="proxy">地址与端口以冒号分开</param>
    ///// <param name="proxyOverride">代理是否跳过本地地址</param>
    //public static void SetIEProxy(String proxy, Boolean proxyOverride = true)
    //{
    //    const Int32 INTERNET_OPTION_PROXY = 38;
    //    const Int32 INTERNET_OPEN_TYPE_PROXY = 3;
    //    const Int32 INTERNET_OPEN_TYPE_DIRECT = 1;

    //    var isCancel = String.IsNullOrEmpty(proxy);

    //    Struct_INTERNET_PROXY_INFO info;

    //    // 填充结构体 
    //    info.dwAccessType = !isCancel ? INTERNET_OPEN_TYPE_PROXY : INTERNET_OPEN_TYPE_DIRECT;
    //    info.proxy = Marshal.StringToHGlobalAnsi("" + proxy);
    //    info.proxyBypass = Marshal.StringToHGlobalAnsi("local");

    //    // 分配内存
    //    var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(info));

    //    // 获取结构体指针
    //    Marshal.StructureToPtr(info, ptr, true);

    //    var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
    //    if (!isCancel)
    //    {
    //        key.SetValue("ProxyServer", proxy);
    //        key.SetValue("ProxyEnable", 1);
    //        if (proxyOverride)
    //            key.SetValue("ProxyOverride", "<local>");
    //        else
    //            key.DeleteValue("ProxyOverride");
    //    }
    //    else
    //        key.SetValue("ProxyEnable", 0);
    //    key.Close();

    //    if (!Runtime.Mono)
    //    {
    //        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, ptr, Marshal.SizeOf(info));

    //        const Int32 INTERNET_OPTION_REFRESH = 0x000025;
    //        const Int32 INTERNET_OPTION_SETTINGS_CHANGED = 0x000027;
    //        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
    //        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    //    }
    //}
    #endregion
}

/// <summary>Http代理事件参数</summary>
public class HttpProxyEventArgs : EventArgs
{
    /// <summary>头部</summary>
    public HttpHeader Header { get; set; }

    /// <summary>主体数据流。外部可以更改，如果只是读取，请一定注意保持指针在原来的位置</summary>
    public Stream Stream { get; set; }

    /// <summary>是否取消操作</summary>
    public Boolean Cancel { get; set; }

    /// <summary>实例化</summary>
    public HttpProxyEventArgs() { }

    /// <summary>实例化</summary>
    /// <param name="header"></param>
    /// <param name="stream"></param>
    public HttpProxyEventArgs(HttpHeader header, Stream stream)
    {
        Header = header;
        Stream = stream;
    }
}