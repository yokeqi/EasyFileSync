using FluentFTP;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Ftp
{
    public class FtpConfig
    {
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public Encoding Encoding { get; set; }

        public static FtpConfig Parse(JObject jconfig)
        {
            if (jconfig == null)
                throw new ArgumentNullException("无效参数 jconfig");

            var ftpConfig = new FtpConfig
            {
                Host = jconfig["host"]?.ToString(),
                UserName = jconfig["user"]?.ToString(),
                Password = jconfig["pass"]?.ToString(),
                Encoding = Encoding.GetEncoding(jconfig["encoding"]?.ToString())
            };

            // 验证连接有效
            var ftp = ftpConfig.CreateFtpClient();
            if (!ftp.IsConnected)
                throw new FtpException($"无法连接Ftp服务器：{ftp.Host}");
            ftp.Disconnect();
            ftp.Dispose();

            return ftpConfig;
        }

        public FtpClient CreateFtpClient(bool isConnect = true)
        {
            var ftp = new FtpClient(Host, UserName, Password)
            {
                Encoding = Encoding
            };
            if (isConnect)
                ftp.Connect();
            return ftp;
        }
    }
}
