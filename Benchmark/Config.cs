using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NewLife;

namespace Benchmark
{
    internal class Config
    {
        #region 属性
        public String Address { get; set; }

        public Int32 Times { get; set; } = 10000;

        /// <summary>并行度</summary>
        public Int32 ConcurrentLevel { get; set; } = 100;

        /// <summary>间隔，毫秒</summary>
        public Int32 Interval { get; set; }

        /// <summary>绑定的本地地址，*表示每一个分开绑定，支持输入一段 10.0.0.31-40，用于海量连接压测</summary>
        public String Bind { get; set; }

        public String Content { get; set; }

        public Boolean Reply { get; set; }
        #endregion

        public void Parse(String[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-n":
                        if (i + 1 < args.Length)
                        {
                            Times = args[i + 1].ToInt();
                            i++;
                        }
                        break;
                    case "-c":
                        if (i + 1 < args.Length)
                        {
                            ConcurrentLevel = args[i + 1].ToInt();
                            i++;
                        }
                        break;
                    case "-i":
                        if (i + 1 < args.Length)
                        {
                            Interval = args[i + 1].ToInt();
                            i++;
                        }
                        break;
                    case "-s":
                        if (i + 1 < args.Length)
                        {
                            Content = args[i + 1];
                            i++;
                        }
                        break;
                    case "-b":
                        if (i + 1 < args.Length)
                        {
                            Bind = args[i + 1];
                            i++;
                        }
                        break;
                    case "-r":
                        Reply = true;
                        break;
                }
            }

            var str = args.LastOrDefault();
            if (!str.StartsWith("-")) Address = str;
        }

        public IPAddress[] GetBinds()
        {
            if (Bind == "*") return NetHelper.GetIPsWithCache();

            var addrs = new List<IPAddress>();
            if (!Bind.IsNullOrEmpty())
            {
                // 支持输入一段 10.0.0.31-40
                var p = Bind.IndexOf('-');
                if (p > 0)
                {
                    var str = Bind.Substring(0, p);
                    var p2 = str.LastIndexOfAny(new[] { '.', ':' });
                    var prefix = str.Substring(0, p2 + 1);
                    var ipv6 = prefix[^1] == ':';
                    var start = str.Substring(p2 + 1).ToInt();
                    var end = Bind.Substring(p + 1).ToInt();
                    for (var i = start; i <= end; i++)
                    {
                        if (ipv6)
                            addrs.Add(IPAddress.Parse(prefix + i.ToString("x")));
                        else
                            addrs.Add(IPAddress.Parse(prefix + i));
                    }
                }
                else
                {
                    var binds = Bind.Split(",");
                    foreach (var item in binds)
                    {
                        addrs.Add(IPAddress.Parse(item));
                    }
                }
            }

            return addrs.ToArray();
        }
    }
}