using System.Collections.Generic;

namespace EasyFileSync.Contracts
{
    public interface ISyncDir
    {
        string Name { get; }
        string FullName { get; }

        ISyncFile[] GetFiles();
        ISyncDir[] GetDirs();
        void Delete();
    }

    public class SyncDirComparer : IEqualityComparer<ISyncDir>
    {
        public bool Equals(ISyncDir x, ISyncDir y) => x.Name == y.Name;

        public int GetHashCode(ISyncDir obj) => obj.Name.GetHashCode();
    }

}
