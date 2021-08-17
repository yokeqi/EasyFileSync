using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core
{
    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 镜像
        /// </summary>
        Mirror,
        /// <summary>
        /// 增量
        /// </summary>
        Append
    }

    /// <summary>
    /// 同步策略
    /// </summary>
    public enum SyncStrategy
    {
        /// <summary>
        /// 文件大小
        /// </summary>
        Size,
        /// <summary>
        /// 最后修改时间
        /// </summary>
        Date,
        /// <summary>
        /// HashCode
        /// </summary>
        HashCode,
    }
}
