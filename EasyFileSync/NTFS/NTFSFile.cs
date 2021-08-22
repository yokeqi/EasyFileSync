using EasyFileSync.Contracts;
using EasyFileSync.Core;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EasyFileSync.NTFS
{
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

        public string GetHash(HashAlgorithm hashAlg = null)
        {
            if (hashAlg == null)
                hashAlg = SHA1.Create();

            using (var stream = File.OpenRead())
            {
                return hashAlg.ComputeHash(stream).Join();
            }
        }

        public long GetSize() => File.Length;
        public void Delete() => File.Delete();

    }
}
