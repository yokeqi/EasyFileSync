using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core.Config
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

            return new FtpConfig
            {
                Host = jconfig["host"]?.ToString(),
                UserName = jconfig["user"]?.ToString(),
                Password = jconfig["pass"]?.ToString(),
                Encoding = Encoding.GetEncoding(jconfig["encoding"]?.ToString())
            };
        }
    }
}
