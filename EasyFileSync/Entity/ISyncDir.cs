using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Entity
{
    public interface ISyncDir
    {
        string Name { get; }
        string FullName { get; }

        ISyncFile[] GetFiles();
        ISyncDir[] GetDirs();
        void Delete();
    }

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

    public class FTPDir : ISyncDir
    {
        private FtpClient _client;
        private FtpListItem _dir;

        public FTPDir(FtpClient client, FtpListItem dir)
        {
            if (client == null || !client.IsConnected)
                throw new ArgumentNullException("无效参数：FtpClient");

            if (dir == null || dir.Type != FtpFileSystemObjectType.Directory)
                throw new ArgumentNullException("无效参数：FtpListItem Dir");

            _dir = dir;
        }

        public string Name => _dir.Name;
        public string FullName => _dir.FullName;

        public ISyncDir[] GetDirs()
        {
            if (!_client.IsConnected)
                throw new FtpException("FTP连接已断开.");

            return _client.GetListing(FullName)
                        .Where(item => item.Type == FtpFileSystemObjectType.Directory)
                        .Select(d => new FTPDir(_client, d)).ToArray();
        }
        public ISyncFile[] GetFiles()
        {
            if (!_client.IsConnected)
                throw new FtpException("FTP连接已断开.");

            return _client.GetListing(FullName)
                        .Where(item => item.Type != FtpFileSystemObjectType.Directory)
                        .Select(f => new FTPFile(_client, f)).ToArray();
        }
        public void Delete()
        {
            if (!_client.IsConnected)
                throw new FtpException("FTP连接已断开.");

            _client.DeleteDirectory(FullName);
        }
    }

    public class SyncDirComparer : IEqualityComparer<ISyncDir>
    {
        public bool Equals(ISyncDir x, ISyncDir y) => x.Name == y.Name;

        public int GetHashCode(ISyncDir obj) => obj.Name.GetHashCode();
    }

}
