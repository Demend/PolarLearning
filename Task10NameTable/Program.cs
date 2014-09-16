using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task10NameTable
{
    public class Program
    {
        public static void Main()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Console.WriteLine("Start NameTable.");
            NameTable nt = new NameTable("../../../Databases/");
            Random rnd = new Random();

            bool toload = true;
            if (toload)
            {
                nt.Clear();
                sw.Restart();
                for (int i = 0; i < 10; i++)
                {
                    HashSet<string> hs = new HashSet<string>();
                    for (int j = 0; j < 4000000; j++)
                    {
                        string s = "s" + rnd.Next(100000000);
                        hs.Add(s);
                    }
                    nt.InsertPortion(hs.ToArray());
                    Console.Write("{0} ", i);
                }
                sw.Stop();
                Console.WriteLine("\nLoad ok. Duration={0}", sw.ElapsedMilliseconds);
            }

            nt.Show();
        }
    }
}
