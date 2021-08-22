using EasyFileSync.Contracts;
using FluentFTP;
using System.Linq;

namespace EasyFileSync.Ftp
{
    public class FluentFTPClient : ISyncClient
    {
        FtpConfig _config;
        FtpClient _client;
        public FluentFTPClient(FtpConfig config)
        {
            _config = config;
            _client = new FtpClient(config.Host, config.UserName, config.Password)
            {
                Encoding = config.Encoding
            };
            _client.AutoConnect();
        }

        public void DeleteDir(string path) => _client.DeleteDirectory(path);
        public void DeleteFile(string path) => _client.DeleteFile(path);

        public ISyncDir[] GetDirs(string path) => _client.GetListing(path)
            .Where(item => item.Type == FtpFileSystemObjectType.Directory)
            .Select(item => new FtpDir(_config, item)).ToArray();

        public ISyncDir[] GetDirs(ISyncDir dir) => GetDirs(dir.FullName);

        public ISyncFile[] GetFiles(ISyncDir dir) => GetFiles(dir.FullName);

        public ISyncFile[] GetFiles(string path) => _client.GetListing(path)
            .Where(item => item.Type != FtpFileSystemObjectType.Directory)
            .Select(item => new FtpFile(_config, item)).ToArray();

        public void UploadDirectory(string src, string tar) => _client.UploadDirectory(src, tar);

        public void UploadFile(string src, string tar) => _client.UploadFile(src, tar);

        public void Dispose()
        {
            if (_client != null)
            {
                if (_client.IsConnected)
                    _client.Disconnect();
                _client.Dispose();
            }
        }
    }
}
