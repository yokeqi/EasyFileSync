using EasyFileSync.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasyFileSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                Print("加载配置中...");
                var configPath = $"app.json";
                if (!File.Exists(configPath))
                {
                    Print($"未检测到配置文件：app.json");
                    return;
                }

                var json = JObject.Parse(File.ReadAllText(configPath));
                var jobs = json["jobs"] as JArray;
                var queue = new ConcurrentQueue<SyncJobBase>();
                Parallel.ForEach(jobs, job =>
                {
                    var client = SyncJobFactory.CreateClient(job as JObject);
                    if (client == null)
                        return;
                    queue.Enqueue(client);
                });
                Print($"加载配置完成，同步任务 {queue.Count} 项...");

                // TODO: 好像记得C#有个可以并发计数的类，可以实现诸如 正在处理(1/3) 的功能
                Parallel.ForEach(queue, client =>
                {
                    client.OutStream += Print;
                    client.Start();
                });
                Print($"全部任务同步完成。");

            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Print(ex.ToString());
            }
            finally
            {
                sw.Stop();
                Print($"总耗时：{sw.Elapsed.TotalMilliseconds}ms.");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }

        }


        static void Print(string output)
        {
            Console.WriteLine($"{DateTime.Now}> {output}");
        }
    }
}
