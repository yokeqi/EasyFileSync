using EasyFileSync.Core.Client;
using EasyFileSync.Core.Config;
using EasyFileSync.Core.Enums;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core.Entity
{
    /*
     * 设计思路：
     * 模拟文件系统对象，针对对象直接操作
     */

    public interface ISyncDir : IDisposable
    {
        string Name { get; }
        string FullName { get; }

        ISyncDir[] GetDirs();
        ISyncFile[] GetFiles();

        void CopyTo(ISyncDir dir);
        void Delete();
    }

    public class SyncDirComparer : IEqualityComparer<ISyncDir>
    {
        public bool Equals(ISyncDir x, ISyncDir y) => x.Name == y.Name;
        public int GetHashCode(ISyncDir obj) => obj.Name.GetHashCode();
    }

    public interface ISyncFile : IDisposable
    {
        string Name { get; }
        string FullName { get; }
        long Size { get; }
        string HashCode { get; }

        string GetModifyDate(string dateFormat = "yyyy-MM-dd hh:mm:ss");

        void CopyTo(ISyncFile file);
        void Delete();
    }

    public class SyncFileComparer : IEqualityComparer<ISyncFile>
    {
        public bool Equals(ISyncFile x, ISyncFile y) => x.Name == y.Name;
        public int GetHashCode(ISyncFile obj) => obj.Name.GetHashCode();
    }

    #region Dir Entity
    /// <summary>
    /// 本地文件夹
    /// </summary>
    public class NTFSDir : ISyncDir
    {
        private DirectoryInfo _dir;
        public NTFSDir(string dirPath) => _dir = new DirectoryInfo(dirPath);
        public NTFSDir(DirectoryInfo dir)
        {
            if (dir == null)
                throw new ArgumentNullException("无效参数：dir");

            _dir = dir;
        }

        public string Name => _dir?.Name;
        public string FullName => _dir?.FullName;

        public void CopyTo(ISyncDir dir)
        {
            // NTFS(this) to NTFS(dir)
            if (dir is NTFSDir)
            {
                // 懒人无敌，既然.NET的DirectoryInfo没有提供CopyTo函数，那就直接调用cmd命令的xcopy
                var cmd = $@"echo d | xcopy ""{FullName}"" ""{dir.FullName}"" /d/e";
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
                var result = proIP.StandardOutput.ReadToEnd();
                System.Diagnostics.Debug.WriteLine($"Copy {FullName} to {dir.FullName},Result:\r\n{result}");
                proIP.Close();
            }

            // NTFS(this) to Ftp(dir)
            if (dir is FtpDir)
            {
                using (var ftp = FtpClientFactory.CreateClient((dir as FtpDir).Config))
                {
                    ftp.UploadDir(this, dir);
                }
            }
        }
        public void Delete() => _dir?.Delete(true);
        public void Dispose() { }

        public ISyncDir[] GetDirs() => _dir.GetDirectories().Select(di => new NTFSDir(di)).ToArray();

        public ISyncFile[] GetFiles() => _dir.GetFiles().Select(file => new NTFSFile(file)).ToArray();
    }

    /// <summary>
    /// Ftp文件夹
    /// </summary>
    public class FtpDir : ISyncDir
    {
        FtpListItem _dir;
        public FtpDir(FtpConfig config, string dirPath) : this(config, new FtpListItem()
        {
            FullName = dirPath,
            Name = new DirectoryInfo(dirPath).Name,
            Type = FtpFileSystemObjectType.Directory
        })
        { }
        public FtpDir(FtpConfig config, FtpListItem dir)
        {
            if (config == null)
                throw new ArgumentNullException("无效参数：config");

            if (dir == null || dir.Type != FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：dir");

            Config = config;
            _dir = dir;
        }

        public FtpConfig Config { get; private set; }
        public string Name => _dir?.Name;
        public string FullName => _dir?.FullName;

        public void CopyTo(ISyncDir dir)
        {
            using (var ftp = CreateFtpClient())
            {
                // Ftp(this) to NTFS(dir)
                if (dir is NTFSDir)
                    ftp.DownloadDir(dir, this);

                // Ftp(this) to Ftp(dir)
                if (dir is FtpDir)
                    ftp.UploadDir(this, dir);
            }
        }
        public void Delete()
        {
            using (var ftp = CreateFtpClient())
            {
                ftp.Delete(this);
            }
        }
        public void Dispose() { }

        public ISyncDir[] GetDirs()
        {
            using (var ftp = CreateFtpClient())
            {
                return ftp.GetDirs(this);
            }
        }
        public ISyncFile[] GetFiles()
        {
            using (var ftp = CreateFtpClient())
            {
                return ftp.GetFiles(this);
            }
        }

        private Client.IFtpClient CreateFtpClient() => FtpClientFactory.CreateClient(Config);
    }
    #endregion

    #region File Entity
    public class NTFSFile : ISyncFile
    {
        FileInfo _file;
        public NTFSFile(string filePath) => _file = new FileInfo(filePath);
        public NTFSFile(FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException("无效参数：fi");

            _file = fi;
        }

        public string Name => _file.Name;
        public string FullName => _file.FullName;
        public long Size => _file.Length;
        public string HashCode
        {
            get
            {
                var alg = SHA1.Create();

                using (var stream = _file.OpenRead())
                {
                    return alg.ComputeHash(stream).Join();
                }
            }
        }

        public void CopyTo(ISyncFile file)
        {
            if (file is NTFSFile)
            {
                _file.CopyTo(file.FullName, true);
            }

            if (file is FtpFile)
            {
                using (var ftp = FtpClientFactory.CreateClient((file as FtpFile).Config))
                {
                    ftp.UploadFile(this, file);
                }
            }

        }
        public void Delete() => _file.Delete();
        public void Dispose() { }

        public string GetModifyDate(string dateFormat = "yyyy-MM-dd hh:mm:ss") => _file.LastWriteTime.ToString(dateFormat);
    }
    public class FtpFile : ISyncFile
    {
        FtpListItem _file;
        public FtpFile(FtpConfig config, string filePath) : this(config, new FtpListItem()
        {
            FullName = filePath,
            Name = new FileInfo(filePath).Name,
            Type = FtpFileSystemObjectType.File
        })
        { }
        public FtpFile(FtpConfig config, FtpListItem file)
        {
            if (config == null)
                throw new ArgumentNullException("无效参数：config");

            if (file == null || file.Type == FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：file");

            _file = file;
            Config = config;
        }

        public FtpConfig Config { get; private set; }
        public string Name => _file.Name;
        public string FullName => _file.FullName;
        public long Size => _file.Size;
        public string HashCode => throw new NotSupportedException("Ftp 暂不支持 Hash");

        public string GetModifyDate(string dateFormat = "yyyy-MM-dd hh:mm") => _file.Modified.ToString(dateFormat);

        public void CopyTo(ISyncFile file)
        {
            using (var ftp = FtpClientFactory.CreateClient(Config))
            {
                if (file is NTFSFile)
                {
                    ftp.DownloadFile(file, this);
                }

                if (file is FtpFile)
                {
                    ftp.UploadFile(this, file);
                }
            }
        }
        public void Delete()
        {
            using (var ftp = FtpClientFactory.CreateClient(Config))
            {
                ftp.Delete(this);
            }
        }

        public void Dispose() { }
    }
    #endregion
}
