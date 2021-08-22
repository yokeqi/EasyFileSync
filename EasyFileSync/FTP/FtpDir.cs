using EasyFileSync.Contracts;
using FluentFTP;
using System;
using System.IO;
using System.Linq;

namespace EasyFileSync.Ftp
{
    public class FtpDir : ISyncDir
    {
        private FtpConfig _ftpConfig;
        private FtpListItem _dir;

        public FtpDir(FtpConfig ftpConfig, FtpListItem dir)
        {
            if (dir == null || dir.Type != FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：FtpListItem Dir");

            _ftpConfig = ftpConfig;
            _dir = dir;
        }
        public FtpDir(FtpConfig ftpConfig, string dir) : this(ftpConfig, new FtpListItem()
        {
            FullName = dir,
            Name = new DirectoryInfo(dir).Name,
            Type = FtpFileSystemObjectType.Directory
        })
        { }

        public string Name => _dir.Name;
        public string FullName => _dir.FullName;

        public ISyncDir[] GetDirs()
        {
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                return ftp.GetListing(FullName)
                            .Where(item => item.Type == FtpFileSystemObjectType.Directory)
                            .Select(d => new FtpDir(_ftpConfig, d)).ToArray();
            }
        }
        public ISyncFile[] GetFiles()
        {
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                return ftp.GetListing(FullName)
                            .Where(item => item.Type != FtpFileSystemObjectType.Directory)
                            .Select(f => new FtpFile(_ftpConfig, f)).ToArray();
            }

        }
        public void Delete()
        {
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                ftp.DeleteDirectory(FullName);
            }
        }
    }
}
