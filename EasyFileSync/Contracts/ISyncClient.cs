using System;

namespace EasyFileSync.Contracts
{
    public interface ISyncClient : IDisposable
    {
        ISyncDir[] GetDirs(string path);
        ISyncDir[] GetDirs(ISyncDir dir);
        ISyncFile[] GetFiles(string path);
        ISyncFile[] GetFiles(ISyncDir dir);

        void UploadFile(string src, string tar);
        void UploadDirectory(string src, string tar);
        void DeleteDir(string path);
        void DeleteFile(string path);
    }
}
