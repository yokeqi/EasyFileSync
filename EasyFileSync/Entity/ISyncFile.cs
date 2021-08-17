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
        long GetSize();
        string GetDate(string dateFormat = "yyyy-MM-dd hh:mm:ss");
        string GetHashCode(HashAlgorithm hashAlg);
    }

    public class NTFSFile : ISyncFile
    {
        protected FileInfo _file;

        public NTFSFile(FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException("无效参数：FileInfo");

            _file = fi;
        }

        public string GetDate(string dateFormat = "yyyy-MM-dd hh:mm:ss") => _file.LastWriteTime.ToString(dateFormat);

        public string GetHashCode(HashAlgorithm hashAlg)
        {
            if (hashAlg == null)
                hashAlg = MD5.Create();

            using (var stream = _file.OpenRead())
            {
                return hashAlg.ComputeHash(stream).Join();
            }
        }

        public long GetSize() => _file.Length;

    }

    public class FTPFile : ISyncFile
    {
        protected FtpListItem _file;
        public FTPFile(FtpListItem fi) => _file = fi;

        public string GetDate(string dateFormat = "yyyy-MM-dd hh:mm") => _file.Modified.ToString(dateFormat);

        public string GetHashCode(HashAlgorithm hashAlg) => throw new NotSupportedException("FTP 暂不支持 HashCode 校验。");

        public long GetSize() => _file.Size;
    }
}
