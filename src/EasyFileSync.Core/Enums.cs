using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core.Enums
{
    /// <summary>
    /// 同步类型
    /// </summary>
    public enum SyncType
    {
        // 本地文件同步
        LocalToLocal = 0,
        /// <summary>
        /// 本地同步到Ftp
        /// </summary>
        LocalToFtp = 1,
        /// <summary>
        /// Ftp同步到本地
        /// </summary>
        FtpToLocal = 2,
        /// <summary>
        /// Ftp胡同不
        /// </summary>
        FtpToFtp = 3
    }

    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 镜像
        /// </summary>
        Mirror = 0,
        /// <summary>
        /// 增量
        /// </summary>
        Append = 1
    }

    /// <summary>
    /// 同步策略
    /// </summary>
    public enum SyncStrategy
    {
        /// <summary>
        /// 文件大小
        /// </summary>
        Size = 0,
        /// <summary>
        /// 最后修改时间
        /// </summary>
        Date = 1,
        /// <summary>
        /// HashCode
        /// </summary>
        HashCode = 2,
    }
}
