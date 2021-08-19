using EasyFileSync.Core;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Entity
{
    public interface ISyncFile
    {
        string Name { get; }
        string FullName { get; }

        long GetSize();
        string GetDate(string dateFormat = "yyyy-MM-dd hh:mm:ss");
        string GetHashCode(HashAlgorithm hashAlg);
        void Delete();
    }

    public class NTFSFile : ISyncFile
    {
        public FileInfo File { get; private set; }
        public string Name => File.Name;
        public string FullName => File.FullName;

        public NTFSFile(FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException("无效参数：FileInfo");

            File = fi;
        }

        public string GetDate(string dateFormat = "yyyy-MM-dd hh:mm:ss") => File.LastWriteTime.ToString(dateFormat);

        public string GetHashCode(HashAlgorithm hashAlg)
        {
            if (hashAlg == null)
                hashAlg = MD5.Create();

            using (var stream = File.OpenRead())
            {
                return hashAlg.ComputeHash(stream).Join();
            }
        }

        public long GetSize() => File.Length;
        public void Delete() => File.Delete();

    }

    public class FTPFile : ISyncFile
    {
        private FtpClient _client;
        private FtpListItem _file;
        public string Name => _file.Name;
        public string FullName => _file.FullName;
        public FTPFile(FtpClient client, FtpListItem fi)
        {
            if (client == null || !client.IsConnected)
                throw new ArgumentNullException("无效参数：FtpClient");

            if (fi == null || fi.Type == FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：FtpListItem File");

            _file = fi;
        }

        public string GetDate(string dateFormat = "yyyy-MM-dd hh:mm") => _file.Modified.ToString(dateFormat);

        public string GetHashCode(HashAlgorithm hashAlg) => throw new NotSupportedException("FTP 暂不支持 HashCode 校验。");

        public long GetSize() => _file.Size;
        public void Delete()
        {
            if (!_client.IsConnected)
                throw new FtpException("FTP连接已断开.");

            _client.DeleteFile(FullName);
        }
    }

    public class SyncFileComparer : IEqualityComparer<ISyncFile>
    {
        public bool Equals(ISyncFile x, ISyncFile y) => x.Name == y.Name;

        public int GetHashCode(ISyncFile obj) => obj.Name.GetHashCode();
    }
}
