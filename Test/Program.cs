using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net.DICOM;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            try
            {
                Test1();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Console.WriteLine("OK!");
            Console.ReadKey();
        }

        static void Test1()
        {
            var server = new DicomServer();
            server.Log = XTrace.Log;
            server.SessionLog = XTrace.Log;
            server.SocketLog = XTrace.Log;
            server.Start();
        }
    }
}