using System;
using System.Linq;

namespace Benchmark
{
    class Config
    {
        #region 属性
        public String Address { get; set; }

        public Int32 Times { get; set; } = 10000;

        public Int32 Thread { get; set; } = 100;

        /// <summary>间隔，毫秒</summary>
        public Int32 Interval { get; set; }

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
                            Thread = args[i + 1].ToInt();
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
                    case "-r":
                        Reply = true;
                        break;
                }
            }

            var str = args.LastOrDefault();
            if (!str.StartsWith("-")) Address = str;
        }
    }
}