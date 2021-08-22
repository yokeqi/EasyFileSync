using System.Collections.Generic;
using System.Security.Cryptography;

namespace EasyFileSync.Contracts
{
    public interface ISyncFile
    {
        string Name { get; }
        string FullName { get; }

        long GetSize();
        string GetDate(string dateFormat = "yyyy-MM-dd hh:mm:ss");
        string GetHash(HashAlgorithm hashAlg = null);
        void Delete();
    }

    public class SyncFileComparer : IEqualityComparer<ISyncFile>
    {
        public bool Equals(ISyncFile x, ISyncFile y) => x.Name == y.Name;

        public int GetHashCode(ISyncFile obj) => obj.Name.GetHashCode();
    }
}
