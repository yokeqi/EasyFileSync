using EasyFileSync.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Entity
{
    public static class SyncClientFactory
    {
        public static SyncClient Create(JObject json)
        {
            if (json == null)
                throw new ArgumentNullException("无效参数：JObject");

            var name = json["name"]?.ToString();

            if (!json.ContainsKey("type"))
                throw new ArgumentException($"{name} 未设置：type");

            if (json.ContainsKey("disable") && json.Value<bool>("disable"))
                return null;

            SyncClient client = null;
            var syncType = json.Value<int>("type");
            switch (syncType)
            {
                case 0:
                    client = new FileToFileSyncClient();
                    break;
                case 1:
                    break;
                case 2:
                    break;
            }

            if (client == null)
                throw new ArgumentException($"{name} 无法解析：type");

            if (json.ContainsKey("mode"))
                client.Mode = (SyncMode)json.Value<int>("mode");

            if (json.ContainsKey("strategy"))
                client.Strategy = (SyncStrategy)json.Value<int>("strategy");

            client.InitDir(json["from"]?.ToString(), json["to"]?.ToString());

            return client;
        }
    }
}
