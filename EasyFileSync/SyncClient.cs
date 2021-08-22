using EasyFileSync.Contracts;
using EasyFileSync.Core;
using EasyFileSync.Ftp;
using EasyFileSync.NTFS;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyFileSync
{
    public abstract class SyncClient
    {
        public delegate void Output(string text);
        public event Output OutStream;

        public ISyncDir From { get; protected set; }
        public ISyncDir To { get; protected set; }
        public SyncMode Mode { get; set; } = SyncMode.Mirror;
        public SyncStrategy Strategy { get; set; } = SyncStrategy.Date;
        public bool IsParallel { get; set; } = true;
        public List<string> IgnoreExts { get; set; } = new List<string>();


        public abstract void InitDir(string from, string to);
        public virtual void Start()
        {
            if (From == null || To == null)
                throw new Exception("From & To 目录未初始化");

            FetchNodes(From, To);
        }
        protected void Print(string output) => OutStream?.Invoke(output);

        protected void FetchNodes(ISyncDir src, ISyncDir tar)
        {
            var srcFiles = src.GetFiles();
            var tarFiles = tar.GetFiles();

            var newFiles = srcFiles.Except(tarFiles, new SyncFileComparer());// 源有目标没有，新增
            Parallel.ForEach(newFiles, file =>
            {
                CopyTo(file, tar);
                var tarPath = Path.Combine(tar.FullName, file.Name);
                Print($"新增文件：{tarPath}");
            });

            if (Mode == SyncMode.Mirror)
            {
                var delFiles = tarFiles.Except(srcFiles, new SyncFileComparer());// 源没有目标有，删除
                Parallel.ForEach(delFiles, file =>
                {
                    file.Delete();
                    Print($"删除文件：{file.FullName}");
                });


                var commonFiles = srcFiles.Intersect(tarFiles, new SyncFileComparer());// 源有目标有，对比更新
                Parallel.ForEach(commonFiles, file =>
                {
                    var temp = tarFiles.FirstOrDefault(f => f.Name == file.Name);
                    if (
                    (Strategy == SyncStrategy.Size && file.GetSize() == temp.GetSize())
                    || (Strategy == SyncStrategy.Date && file.GetDate() == temp.GetDate())
                    || (Strategy == SyncStrategy.HashCode && file.GetHash() == temp.GetHash())
                    )
                        return;

                    CopyTo(file, tar);
                    var tarPath = Path.Combine(tar.FullName, file.Name);
                    Print($"更新文件：{tarPath}");
                });
            }

            // 继续遍历文件夹
            var srcDirs = src.GetDirs();
            var tarDirs = tar.GetDirs();

            var newDirs = srcDirs.Except(tarDirs, new SyncDirComparer());// 源有目标没有，新增
            Parallel.ForEach(newDirs, dir =>
            {
                CopyTo(dir, tar);
                var tarPath = Path.Combine(tar.FullName, dir.Name);
                Print($"新增文件夹：{tarPath}");
            });

            if (Mode == SyncMode.Mirror)
            {
                // 源没有目标有，删除
                var delDirs = tarDirs.Except(srcDirs, new SyncDirComparer());
                Parallel.ForEach(delDirs, dir =>
                {
                    dir.Delete();
                    Print($"删除文件夹：{dir.FullName}");
                });

                // 源有目标有，递归对比
                var commonDirs = srcDirs.Intersect(tarDirs, new SyncDirComparer());
                Parallel.ForEach(commonDirs, dir =>
                {
                    var temp = tarDirs.FirstOrDefault(f => f.Name == dir.Name);
                    FetchNodes(dir, temp);
                });
            }
        }

        protected abstract void CopyTo(ISyncDir src, ISyncDir parentDir);
        protected abstract void CopyTo(ISyncFile src, ISyncDir parentDir);
    }

    public class FileToFileSyncClient : SyncClient
    {
        protected override void CopyTo(ISyncDir src, ISyncDir parentDir)
        {
            // 懒人无敌，既然.NET的DirectoryInfo没有提供CopyTo函数，那就直接调用cmd命令的xcopy
            var tarPath = Path.Combine(parentDir.FullName, src.Name);
            var cmd = $@"echo d | xcopy ""{src.FullName}"" ""{tarPath}"" /d/e";
            //System.Diagnostics.Process.Start("cmd.exe", cmd);
            System.Diagnostics.Process proIP = new System.Diagnostics.Process();
            proIP.StartInfo.FileName = "cmd.exe";
            proIP.StartInfo.UseShellExecute = false;
            proIP.StartInfo.RedirectStandardInput = true;
            proIP.StartInfo.RedirectStandardOutput = true;
            proIP.StartInfo.RedirectStandardError = true;
            proIP.StartInfo.CreateNoWindow = true;
            proIP.Start();
            proIP.StandardInput.WriteLine(cmd);
            proIP.StandardInput.WriteLine("exit");
            string strResult = proIP.StandardOutput.ReadToEnd();
            proIP.Close();
        }

        protected override void CopyTo(ISyncFile src, ISyncDir parentDir)
        {
            var tarPath = Path.Combine(parentDir.FullName, src.Name);
            (src as NTFSFile).File.CopyTo(tarPath, true);
        }

        public override void InitDir(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || !Directory.Exists(from))
                throw new ArgumentNullException("无效参数 from。");
            if (string.IsNullOrWhiteSpace(to) || !Directory.Exists(to))
                throw new ArgumentNullException("无效参数 to");

            this.From = new NTFSDir(from);
            this.To = new NTFSDir(to);
        }
    }

    public class FileToFtpSyncClient : SyncClient
    {
        public FtpConfig _ftpConfig;
        public FileToFtpSyncClient(FtpConfig ftpClient)
        {
            _ftpConfig = ftpClient;
        }

        public override void InitDir(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || !Directory.Exists(from))
                throw new ArgumentNullException("无效参数 from。");
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentNullException("无效参数 to");

            this.From = new NTFSDir(from);
            this.To = new FtpDir(_ftpConfig, to);
        }

        protected override void CopyTo(ISyncDir src, ISyncDir parentDir)
        {
            var tarPath = Path.Combine(parentDir.FullName, src.Name);
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                ftp.UploadDirectory(src.FullName, tarPath, FtpFolderSyncMode.Mirror);
            }
        }

        protected override void CopyTo(ISyncFile src, ISyncDir parentDir)
        {
            var tarPath = Path.Combine(parentDir.FullName, src.Name).Replace("\\", "/");
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                // TODO:上传上去时间就变成最新的了，如何保持上传后文件属性保留？类似Beyond Compare
                ftp.UploadFile(src.FullName, tarPath);
            }
        }
    }

    public class FtpToFileSyncClient : SyncClient
    {
        public FtpConfig _ftpConfig;
        public FtpToFileSyncClient(FtpConfig config)
        {
            _ftpConfig = config;
        }

        public override void InitDir(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from))
                throw new ArgumentNullException("无效参数 from。");
            if (string.IsNullOrWhiteSpace(to) || !Directory.Exists(to))
                throw new ArgumentNullException("无效参数 to");

            this.From = new FtpDir(_ftpConfig, from);
            this.To = new NTFSDir(to);
        }

        protected override void CopyTo(ISyncDir src, ISyncDir parentDir)
        {
            var tarPath = Path.Combine(parentDir.FullName, src.Name);
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                ftp.DownloadDirectory(tarPath, src.FullName, FtpFolderSyncMode.Mirror);
            }
        }

        protected override void CopyTo(ISyncFile src, ISyncDir parentDir)
        {
            var tarPath = Path.Combine(parentDir.FullName, src.Name);
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                ftp.DownloadFile(tarPath, src.FullName);
            }
        }
    }
}
