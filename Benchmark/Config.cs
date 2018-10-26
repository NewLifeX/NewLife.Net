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

        public String Content { get; set; }
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
                    case "-s":
                        if (i + 1 < args.Length)
                        {
                            Content = args[i + 1];
                            i++;
                        }
                        break;
                }
            }

            var str = args.LastOrDefault();
            if (!str.StartsWith("-")) Address = str;
        }
    }
}