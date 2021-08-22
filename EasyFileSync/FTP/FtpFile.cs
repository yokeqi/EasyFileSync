using EasyFileSync.Contracts;
using FluentFTP;
using System;
using System.Security.Cryptography;

namespace EasyFileSync.Ftp
{
    public class FtpFile : ISyncFile
    {
        private FtpConfig _ftpConfig;
        private FtpListItem _file;
        public string Name => _file.Name;
        public string FullName => _file.FullName;
        public FtpFile(FtpConfig config, FtpListItem fi)
        {
            if (fi == null || fi.Type == FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：FtpListItem File");

            _ftpConfig = config;
            _file = fi;
        }

        public string GetDate(string dateFormat = "yyyy-MM-dd hh:mm") => _file.Modified.ToString(dateFormat);

        public string GetHash(HashAlgorithm hashAlg = null) =>  throw new NotSupportedException("FTP 暂不支持 HashCode 校验。");

        public long GetSize() => _file.Size;
        public void Delete()
        {
            using (var ftp = _ftpConfig.CreateFtpClient())
            {
                ftp.DeleteFile(FullName);
            }
        }
    }
}
