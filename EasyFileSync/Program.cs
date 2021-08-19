using EasyFileSync.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFileSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var src = $@"H:\Code Repo\book\skill";
            var tar = $@"E:\www\docs";

            try
            {
                var client = new FileToFileSyncClient()
                {
                    From = new NTFSDir(src),
                    To = new NTFSDir(tar)
                };
                client.OutStream += Print;
                client.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Print(ex.ToString());
            }
            finally
            {
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
