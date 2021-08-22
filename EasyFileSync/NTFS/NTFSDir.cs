using EasyFileSync.Contracts;
using System;
using System.IO;
using System.Linq;

namespace EasyFileSync.NTFS
{
    public class NTFSDir : ISyncDir
    {
        private DirectoryInfo _di;
        public NTFSDir(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                throw new ArgumentNullException("无效参数：dirPath");

            _di = new DirectoryInfo(dirPath);
        }
        public NTFSDir(DirectoryInfo di)
        {
            if (di == null)
                throw new ArgumentNullException("无效参数：DirectoryInfo");

            _di = di;
        }

        public string Name => _di.Name;
        public string FullName => _di.FullName;

        public ISyncDir[] GetDirs() => _di.GetDirectories().Select(d => new NTFSDir(d)).ToArray();
        public ISyncFile[] GetFiles() => _di.GetFiles().Select(f => new NTFSFile(f)).ToArray();
        public void Delete() => _di.Delete(true);
    }
}
