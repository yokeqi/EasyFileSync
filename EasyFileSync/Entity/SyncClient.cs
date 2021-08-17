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
            var newFiles = srcFiles.Except(tarFiles);// 源有目标没有，新增
            if (Mode == SyncMode.Mirror)
            {
                var delFiles = tarFiles.Except(srcFiles);// 源没有目标有，删除
                var commonFiles = srcFiles.Intersect(tarFiles);// 源有目标有，对比更新
            }

            // 源有目标没有，新增
            Parallel.ForEach(srcFiles, file =>
            {
                Print($"匹配：{file.FullName}");

                if (tarFiles.Any(f => f.Name == file.Name))
                    return;

                var tarPath = Path.Combine(tar.FullName, file.Name);
                file.CopyTo(tarPath, true);
                Print($"新增：{tarPath}");
            });


            if (this.Mode == SyncMode.Mirror)
            {
                // 源有目标有，比对更新
                Parallel.ForEach(srcFiles, file =>
                {
                    Print($"匹配：{file.FullName}");

                    var temp = tarFiles.FirstOrDefault(f => f.Name == file.Name);
                    if (temp == null)
                        return;

                    var srcFile = new NTFSFile(file);
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


                // 源没有目标有，删除
                Parallel.ForEach(tarFiles, file =>
                {
                    if (srcFiles.Any(f => f.Name == file.Name))
                        return;

                    file.Delete();
                    Print($"删除文件：{file.FullName}");
                });
            }


            // 继续遍历文件夹
            var srcDirs = src.GetDirectories();
            var tarDirs = tar.GetDirectories();
            // 源有目标没有，新增
            var newDirs = srcDirs.Where(d => tarDirs.Any(d1 => d1.Name == d.Name));
            Parallel.ForEach(srcDirs, dir =>
            {
                if (tarDirs.Any(d => d.Name == dir.Name))
                    return;

                var tarPath = Path.Combine(tar.FullName, dir.Name);
                var cmd = $@"xcopy ""{dir.FullName}"" ""{tarPath}"" /d/e";
                //System.Diagnostics.Process.Start("cmd.exe", cmd);
                Print($"复制文件夹：{cmd}");
            });

            if (Mode == SyncMode.Mirror)
            {
                // 源没有目标有，删除
            }
        }
    }

}
