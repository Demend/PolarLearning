using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.Data;


namespace Task09ExtremeIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start Extreme Index");
            DataSet ds = new DataSet(@"..\..\..\Databases\");
            Random rnd = new Random();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            int npersons = 100000;
            bool toload = true;
            if (toload)
            {
                sw.Restart();
                ds.LoadXML((new TestDataGenerator(npersons, 777777)).Generate());
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds); // 566
            }
            else
            {
                sw.Restart();
                ds.Warmup();
                sw.Stop();
                Console.WriteLine("Warmup ok. Duration={0}", sw.ElapsedMilliseconds); // 566
            }

            for (int j = 0; j < 10; j++)
            {
                sw.Restart();
                for (int i = 0; i < 1000; i++)
                {
                    ds.GetRelationByPerson(rnd.Next(npersons - 1));
                }
                sw.Stop();
                Console.Write("{0} ", sw.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }
    }
}
