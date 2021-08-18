using EasyFileSync.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Entity
{
    public abstract class SyncClient
    {
        public delegate void Output(string text);
        public event Output OutStream;

        public string From { get; set; }
        public string To { get; set; }
        public SyncMode Mode { get; set; } = SyncMode.Mirror;
        public SyncStrategy Strategy { get; set; } = SyncStrategy.Date;
        public bool IsParallel { get; set; } = true;
        public List<string> IgnoreExts { get; set; } = new List<string>();

        public abstract void Start();
        protected void Print(string output) => OutStream?.Invoke(output);
    }

    public class FileToFileSyncClient : SyncClient
    {
        public override void Start()
        {
            if (!Directory.Exists(From))
            {
                Print($"文件夹不存在：{From}");
                return;
            }

            if (!Directory.Exists(To))
            {
                Print($"文件夹不存在：{To}");
                return;
            }

            FetchNodes(new DirectoryInfo(From), new DirectoryInfo(To));
        }

        private void FetchNodes(DirectoryInfo src, DirectoryInfo tar)
        {
            var srcFiles = src.GetFiles();
            var tarFiles = tar.GetFiles();

            // TODO:改成集合操作（交、差）集来处理
            var newFiles = srcFiles.Except(tarFiles, new FileInfoComparer());// 源有目标没有，新增
            Parallel.ForEach(newFiles, file =>
            {
                var tarPath = Path.Combine(tar.FullName, file.Name);
                file.CopyTo(tarPath, true);
                Print($"新增：{tarPath}");
            });


            if (Mode == SyncMode.Mirror)
            {
                var delFiles = tarFiles.Except(srcFiles, new FileInfoComparer());// 源没有目标有，删除
                Parallel.ForEach(delFiles, file =>
                {
                    file.Delete();
                    Print($"删除文件：{file.FullName}");
                });

                var commonFiles = srcFiles.Intersect(tarFiles, new FileInfoComparer());// 源有目标有，对比更新
                Parallel.ForEach(commonFiles, file =>
                {
                    var srcFile = new NTFSFile(file);
                    var temp = tarFiles.FirstOrDefault(f => f.Name == file.Name);
                    var tarFile = new NTFSFile(temp);
                    if (
                    (Strategy == SyncStrategy.Size && srcFile.GetSize() == tarFile.GetSize())
                    || (Strategy == SyncStrategy.Date && srcFile.GetDate() == tarFile.GetDate())
                    || (Strategy == SyncStrategy.HashCode && srcFile.GetHashCode() == tarFile.GetHashCode())
                    )
                        return;

                    var tarPath = Path.Combine(tar.FullName, file.Name);
                    file.CopyTo(tarPath, true);
                    Print($"更新：{tarPath}");
                });
            }


            // 继续遍历文件夹
            var srcDirs = src.GetDirectories();
            var tarDirs = tar.GetDirectories();

            var newDirs = srcDirs.Except(tarDirs, new DirectoryComparer());// 源有目标没有，新增
            Parallel.ForEach(newDirs, dir =>
            {
                // 懒人无敌，既然.NET的DirectoryInfo没有提供CopyTo函数，那就直接调用cmd命令的xcopy
                var tarPath = Path.Combine(tar.FullName, dir.Name);
                var cmd = $@"echo d | xcopy ""{dir.FullName}"" ""{tarPath}"" /d/e";
                //System.Diagnostics.Process.Start("cmd.exe", cmd);
                System.Diagnostics.Process proIP = new System.Diagnostics.Process();
                proIP.StartInfo.FileName = "cmd.exe";
                proIP.StartInfo.UseShellExecute = false;
                proIP.StartInfo.RedirectStandardInput = true;
                proIP.StartInfo.RedirectStandardOutput = true;
                proIP.StartInfo.RedirectStandardError = true;
                proIP.StartInfo.CreateNoWindow = true;
                proIP.Start();
                proIP.StandardInput.WriteLine(cmd);
                proIP.StandardInput.WriteLine("exit");
                string strResult = proIP.StandardOutput.ReadToEnd();
                proIP.Close();
                Print($"复制文件夹：{tarPath}");
            });

            if (Mode == SyncMode.Mirror)
            {
                // 源没有目标有，删除
                var delDirs = tarDirs.Except(srcDirs, new DirectoryComparer());
                Parallel.ForEach(delDirs, dir =>
                {
                    dir.Delete(true);
                    Print($"删除文件夹：{dir.FullName}");
                });

                // 源有目标有，递归对比
                var commonDirs = srcDirs.Intersect(tarDirs, new DirectoryComparer());
                Parallel.ForEach(commonDirs, dir =>
                {
                    var temp = tarDirs.FirstOrDefault(f => f.Name == dir.Name);
                    FetchNodes(dir, temp);
                });
            }
        }

        class FileInfoComparer : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo x, FileInfo y) => x.Name == y.Name;

            public int GetHashCode(FileInfo obj) => obj.Name.GetHashCode();
        }
        class DirectoryComparer : IEqualityComparer<DirectoryInfo>
        {
            public bool Equals(DirectoryInfo x, DirectoryInfo y) => x.Name == y.Name;

            public int GetHashCode(DirectoryInfo obj) => obj.Name.GetHashCode();
        }
    }

}
