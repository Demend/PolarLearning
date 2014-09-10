using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task06TestGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start TestGenerator");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            MySQL db = new MySQL("server=localhost;uid=root;");
            //SQLite db = new SQLite("Data Source=D:\\home\\dev2012\\PolarLearning\\Task06TestGenerator\\test.db3");
            //SQLdatabase db = new SQLdatabase(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\Александр\Documents\test20140831.mdf;Integrated Security=True;Connect Timeout=30");

            int npersons = 100000;
            bool toload = false;
            if (toload)
            {
                sw.Restart();
                db.PrepareToLoad();
                Polar.Data.TestDataGenerator generator = new Polar.Data.TestDataGenerator(npersons, 777777);
                db.LoadElementFlow(generator.Generate());
                db.MakeIndexes();
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds); // 10000: 14.9 сек.
            }

            sw.Restart();
            db.Count("person");
            sw.Stop();
            Console.WriteLine("Count ok. Duration={0}", sw.ElapsedMilliseconds); // 1200

            sw.Restart();
            db.SelectById(2870, "person");
            //db.SelectById(1309, "person");
            sw.Stop();
            Console.WriteLine("SelectById ok. Duration={0}", sw.ElapsedMilliseconds); // 7

            sw.Restart();
            db.SearchByName("Pupkin999", "person");
            sw.Stop();
            Console.WriteLine("SearchByName ok. Duration={0}", sw.ElapsedMilliseconds); // 7

            sw.Restart();
            db.GetRelationByPerson(2870);
            //db.GetRelation(1309, "reflected");
            sw.Stop();
            Console.WriteLine("GetRelation ok. Duration={0}", sw.ElapsedMilliseconds); // 91

            // Проверка серии портретов
            Random rnd = new Random(999999); int i = 0;
            sw.Restart();
            for (; i < 100; i++) db.GetRelationByPerson(rnd.Next(10000));
            sw.Stop();
            System.Console.WriteLine("GetPortraitById OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 1227 ms. 

            // Проверка серии портретов (2) по тем же идентификаторам
            rnd = new Random(999999); i = 0;
            sw.Restart();
            for (; i < 100; i++) db.GetRelationByPerson(rnd.Next(10000));
            sw.Stop();
            System.Console.WriteLine("GetPortraitById OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 43 ms. 

            // 
            Console.WriteLine("==== Динамика естественного разогрева ====");
            Random rnd2 = new Random();
            for (int j = 0; j < 100; j++)
            {
                sw.Restart();
                for (int ii = 0; ii < 100; ii++) db.GetRelationByPerson(rnd2.Next(npersons - 1));
                sw.Stop();
                Console.Write("{0} ", sw.ElapsedMilliseconds);
            }
            System.Console.WriteLine(); // 
        }
    }
}
