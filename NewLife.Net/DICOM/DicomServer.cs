using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Net.DICOM
{
    /// <summary>DICOM服务端。管理所有网络会话</summary>
    public class DicomServer: NetServer<DicomSession>
    {
        /// <summary>实例化服务端</summary>
        public DicomServer()
        {
            // 默认端口
            Port = 104;
        }
    }
}