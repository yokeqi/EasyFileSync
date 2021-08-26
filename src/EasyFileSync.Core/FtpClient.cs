using EasyFileSync.Core.Config;
using EasyFileSync.Core.Entity;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core.Client
{
    public static class FtpClientFactory
    {
        public static IFtpClient CreateClient(FtpConfig config) => new FluntFtpClient(config);
    }

    /// <summary>
    /// 客户端接口，统一采用短链接方式，支持并发处理
    /// </summary>
    public interface IFtpClient : IDisposable
    {
        ISyncDir[] GetDirs(ISyncDir dir);
        ISyncFile[] GetFiles(ISyncDir dir);

        void UploadDir(ISyncDir local, ISyncDir remote);
        void UploadFile(ISyncFile local, ISyncFile remote);

        void DownloadDir(ISyncDir local, ISyncDir remote);
        void DownloadFile(ISyncFile local, ISyncFile remote);

        void Delete(ISyncDir dir);
        void Delete(ISyncFile file);
    }
    public class FluntFtpClient : IFtpClient
    {
        FtpConfig _config;
        public FluntFtpClient(FtpConfig config)
        {
            if (config == null)
                throw new ArgumentNullException("无效参数：config");

            _config = config;
        }

        public void Delete(ISyncDir dir)
        {
            using (var ftp = CreateClient())
            {
                ftp.DeleteDirectory(dir.FullName);
            }
        }

        public void Delete(ISyncFile file)
        {
            using (var ftp = CreateClient())
            {
                ftp.DeleteFile(file.FullName);
            }
        }

        public ISyncDir[] GetDirs(ISyncDir dir)
        {
            using (var ftp = CreateClient())
            {
                return ftp.GetListing(dir.FullName).Where(item => item.Type == FtpFileSystemObjectType.Directory).Select(item => new FtpDir(_config, item)).ToArray();
            }
        }

        public ISyncFile[] GetFiles(ISyncDir dir)
        {
            using (var ftp = CreateClient())
            {
                return ftp.GetListing(dir.FullName).Where(item => item.Type != FtpFileSystemObjectType.Directory).Select(item => new FtpFile(_config, item)).ToArray();
            }
        }

        public void UploadDir(ISyncDir local, ISyncDir remote)
        {
            using (var ftp = CreateClient())
            {
                // NTFS to Ftp
                if (local is NTFSDir)
                {
                    ftp.UploadDirectory(local.FullName, remote.FullName, FtpFolderSyncMode.Mirror);
                }

                // Ftp(src) to Ftp(tar)
                if (local is FtpDir)
                {
                    using (var srcClient = CreateClient((local as FtpDir).Config))
                    {
                        // TODO：这个有待测试
                        srcClient.TransferDirectory(local.FullName, ftp, remote.FullName, FtpFolderSyncMode.Mirror);
                    }
                }
            }
        }

        public void UploadFile(ISyncFile local, ISyncFile remote)
        {
            using (var ftp = CreateClient())
            {
                // NTFS to Ftp
                if (local is NTFSFile)
                {
                    ftp.UploadFile(local.FullName, remote.FullName);
                }

                // Ftp(src) to Ftp(tar)
                if (local is FtpFile)
                {
                    using (var srcClient = CreateClient((local as FtpFile).Config))
                    {
                        // TODO：这个有待测试
                        srcClient.TransferFile(local.FullName, ftp, remote.FullName, existsMode: FtpRemoteExists.Overwrite);
                    }
                }
            }
        }

        public void DownloadDir(ISyncDir local, ISyncDir remote)
        {
            using (var ftp = CreateClient())
            {
                // Ftp(tar) to NTFS(src)
                if (local is NTFSDir)
                {
                    ftp.DownloadDirectory(local.FullName, remote.FullName, FtpFolderSyncMode.Mirror);
                }

                // Ftp(tar) to Ftp(src)
                if (local is FtpDir)
                {
                    // 将当前Ftp目录下载到另一个Ftp目录
                    using (var srcClient = CreateClient((local as FtpDir).Config))
                    {
                        ftp.TransferDirectory(remote.FullName, srcClient, local.FullName, FtpFolderSyncMode.Mirror);
                    }
                }
            }
        }

        public void DownloadFile(ISyncFile local, ISyncFile remote)
        {
            using (var ftp = CreateClient())
            {
                // Ftp(tar) to NTFS(src)
                if (local is NTFSFile)
                {
                    ftp.DownloadFile(local.FullName, remote.FullName);
                }

                // Ftp(tar) to Ftp(src)
                if (local is FtpFile)
                {
                    using (var srcClient = CreateClient((local as FtpFile).Config))
                    {
                        ftp.TransferFile(remote.FullName, srcClient, local.FullName, existsMode: FtpRemoteExists.Overwrite);
                    }
                }
            }
        }

        public FtpClient CreateClient(FtpConfig config = null, bool isConnect = true)
        {
            if (config == null)
                config = _config;// TODO：暂时就先这么设计吧，也没啥地方用到

            var ftp = new FtpClient(config.Host, config.UserName, config.Password)
            {
                Encoding = config.Encoding
            };
            ftp.AutoConnect();
            if (isConnect && !ftp.IsConnected)
                ftp.Connect();
            return ftp;
        }

        public void Dispose() { }
    }
}
