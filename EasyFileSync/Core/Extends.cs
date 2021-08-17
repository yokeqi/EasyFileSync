using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync.Core
{
    public static class CoreExtends
    {
        #region Collection
        /// <summary>
        /// 是否为null或数量是否为0
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this T[] array)
        {
            return (array == null || array.Length <= 0);
        }

        /// <summary>
        /// 是否为null或数量是否为0
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsEmpty(this ICollection collection)
        {
            return (collection == null || collection.Count <= 0);
        }
        #endregion

        #region Byte
        /// <summary>
        /// 按照特定格式拼接成字符串形式
        /// </summary>
        /// <param name="data"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Join(this byte[] data, string format = "X2")
        {
            if (data.IsEmpty())
                return string.Empty;

            return string.Join("", data.Select(d => d.ToString(format)));
        }

        public static string ToEncodingString(this byte[] data, Encoding encoding = null)
        {
            if (data.IsEmpty())
                return string.Empty;

            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(data);
        }
        #endregion
    }
}
