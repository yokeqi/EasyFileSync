using EasyFileSync.Core.Client;
using EasyFileSync.Core.Config;
using EasyFileSync.Core.Entity;
using EasyFileSync.Core.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core
{
    public static class SyncJobFactory
    {
        public static SyncJobBase CreateClient(JObject config)
        {
            if (config == null)
                throw new ArgumentNullException("无效参数：config");

            var name = config["name"]?.ToString();

            if (config.ContainsKey("disable") && config.Value<bool>("disable"))
                return null;

            if (!config.ContainsKey("type"))
                throw new ArgumentNullException($"{name} 未设置：type");

            var sType = (SyncType)config.Value<int>("type");

            SyncJobBase client = null;
            switch (sType)
            {
                case SyncType.LocalToLocal:
                    client = new LocalToLocalJob(config);
                    break;
                case SyncType.LocalToFtp:
                    client = new LocalToFtpJob(config);
                    break;
                case SyncType.FtpToLocal:
                    client = new FtpToLocalJob(config);
                    break;
                case SyncType.FtpToFtp:
                    client = new FtpToFtpJob(config);
                    break;
            }

            if (client == null)
                throw new Exception($"{name} 无法实例化：{sType}");

            if (config.ContainsKey("mode"))
                client.Mode = (SyncMode)config.Value<int>("mode");

            if (config.ContainsKey("strategy"))
                client.Strategy = (SyncStrategy)config.Value<int>("strategy");

            return client;
        }
    }

    public abstract class SyncJobBase : IDisposable
    {
        public delegate void Output(string text);
        public event Output OutStream;

        public ISyncDir From { get; protected set; }
        public ISyncDir To { get; protected set; }
        public SyncMode Mode { get; set; }
        public SyncStrategy Strategy { get; set; }

        public virtual void Start()
        {
            if (From == null || To == null)
                throw new Exception("From & To 目录未初始化");

            Sync(From, To);
        }

        protected void Sync(ISyncDir src, ISyncDir tar)
        {
            var fileComparer = new SyncFileComparer();
            var srcFiles = src.GetFiles();
            var tarFiles = tar.GetFiles();

            var newFiles = srcFiles.Except(tarFiles, fileComparer);// 源有目标没有，新增
            Parallel.ForEach(newFiles, file =>
            {
                CopyTo(file, tar);

                var tarPath = Path.Combine(tar.FullName, file.Name);
                Print($"新增文件：{tarPath}");
            });

            if (Mode == SyncMode.Mirror)
            {
                var delFiles = tarFiles.Except(srcFiles, fileComparer);// 源没有目标有，删除
                Parallel.ForEach(delFiles, file =>
                {
                    file.Delete();
                    Print($"删除文件：{file.FullName}");
                });


                var commonFiles = srcFiles.Intersect(tarFiles, fileComparer);// 源有目标有，对比更新
                Parallel.ForEach(commonFiles, file =>
                {
                    var temp = tarFiles.FirstOrDefault(f => f.Name == file.Name);
                    if (Equals(file, temp, Strategy))
                        return;

                    CopyTo(file, tar);

                    var tarPath = Path.Combine(tar.FullName, file.Name);
                    Print($"更新文件：{tarPath}");
                });
            }

            // 继续遍历文件夹
            var dirComparer = new SyncDirComparer();
            var srcDirs = src.GetDirs();
            var tarDirs = tar.GetDirs();

            var newDirs = srcDirs.Except(tarDirs, dirComparer);// 源有目标没有，新增
            Parallel.ForEach(newDirs, dir =>
            {
                CopyTo(dir, tar);

                var tarPath = Path.Combine(tar.FullName, dir.Name);
                Print($"新增文件夹：{tarPath}");
            });

            if (Mode == SyncMode.Mirror)
            {
                // 源没有目标有，删除
                var delDirs = tarDirs.Except(srcDirs, dirComparer);
                Parallel.ForEach(delDirs, dir =>
                {
                    dir.Delete();
                    Print($"删除文件夹：{dir.FullName}");
                });

                // 源有目标有，递归对比
                var commonDirs = srcDirs.Intersect(tarDirs, dirComparer);
                Parallel.ForEach(commonDirs, dir =>
                {
                    var temp = tarDirs.FirstOrDefault(f => f.Name == dir.Name);
                    Sync(dir, temp);
                });
            }
        }

        protected abstract void CopyTo(ISyncDir src, ISyncDir tarParent);
        protected abstract void CopyTo(ISyncFile src, ISyncDir tarParent);

        protected bool Equals(ISyncFile src, ISyncFile tar, SyncStrategy strategy = SyncStrategy.Date)
        {
            switch (strategy)
            {
                case SyncStrategy.Size:
                    return src.Size == tar.Size;
                case SyncStrategy.HashCode:
                    return src.HashCode == tar.HashCode;
                case SyncStrategy.Date:
                    var dateFormat = "yyyy-MM-dd hh:mm:ss";
                    if (src is FtpFile || tar is FtpFile)
                        dateFormat = "yyyy-MM-dd hh:mm";
                    return src.GetModifyDate(dateFormat) == tar.GetModifyDate(dateFormat);
            }

            return false;
        }
        protected void Print(string output) => OutStream?.Invoke(output);
        public virtual void Dispose() { }
    }

    public class LocalToLocalJob : SyncJobBase
    {
        public LocalToLocalJob(JObject config)
        {
            From = new NTFSDir(config["from"].ToString());
            To = new NTFSDir(config["to"].ToString());
        }

        protected override void CopyTo(ISyncDir src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            src.CopyTo(new NTFSDir(tar));
        }

        protected override void CopyTo(ISyncFile src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            src.CopyTo(new NTFSFile(tar));
        }
    }
    public class LocalToFtpJob : SyncJobBase
    {
        public LocalToFtpJob(JObject config)
        {
            From = new NTFSDir(config["from"].ToString());

            var jTo = config["to"] as JObject;
            To = new FtpDir(FtpConfig.Parse(jTo), jTo["path"].ToString());
        }

        protected override void CopyTo(ISyncDir src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            var config = (To as FtpDir).Config;
            src.CopyTo(new FtpDir(config, tar));
        }

        protected override void CopyTo(ISyncFile src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            var config = (To as FtpDir).Config;
            src.CopyTo(new FtpFile(config, tar));
        }
    }
    public class FtpToLocalJob : SyncJobBase
    {
        public FtpToLocalJob(JObject config)
        {
            var jFrom = config["from"] as JObject;
            From = new FtpDir(FtpConfig.Parse(jFrom), jFrom["path"].ToString());

            To = new NTFSDir(config["to"].ToString());
        }

        protected override void CopyTo(ISyncDir src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            src.CopyTo(new NTFSDir(tar));
        }

        protected override void CopyTo(ISyncFile src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            src.CopyTo(new NTFSFile(tar));
        }
    }
    public class FtpToFtpJob : SyncJobBase
    {
        public FtpToFtpJob(JObject config)
        {
            var jFrom = config["from"] as JObject;
            From = new FtpDir(FtpConfig.Parse(jFrom), jFrom["path"].ToString());

            var jTo = config["to"] as JObject;
            To = new FtpDir(FtpConfig.Parse(jTo), jTo["path"].ToString());
        }

        protected override void CopyTo(ISyncDir src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            var config = (To as FtpDir).Config;
            src.CopyTo(new FtpDir(config, tar));
        }

        protected override void CopyTo(ISyncFile src, ISyncDir tarParent)
        {
            var tar = Path.Combine(tarParent.FullName, src.Name);
            var config = (To as FtpDir).Config;
            src.CopyTo(new FtpFile(config, tar));
        }
    }
}
