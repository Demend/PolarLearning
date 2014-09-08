using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Task07ORM
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ORM starts.");
            string path = "../../../Databases/";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            // Приконнектимся или создадим базу данных
            Database db = new Database(path, XElement.Parse(schema_str));
            // Загрузим тестовые данные
            int npersons = 100000;
            bool toload = true;
            if (toload)
            {
                sw.Restart();
                Polar.Data.TestDataGenerator tdg = new Polar.Data.TestDataGenerator(npersons, 777777);
                db.LoadXML(tdg.Generate());
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds); // для 10 тыс.: 4.5 сек.
            }

            // Поиск по имени
            bool tosearch = true;
            if (tosearch)
            {
                sw.Restart();
                var query = db.SearchByNameIn("Пупкин111", "person");
                Console.WriteLine(query.Count());
                sw.Stop();
                Console.WriteLine("SearchByNameIn ok. Duration={0}", sw.ElapsedMilliseconds); // 322, 298 мс.
            }

            // Простой портрет
            sw.Restart();
            var portr = db.GetPortraitByIdIn(2870, "person");
            if (portr != null) Console.WriteLine(portr.ToString());
            sw.Stop();
            Console.WriteLine("GetPortraitByIdIn ok. Duration={0}", sw.ElapsedMilliseconds); // 7 мс.

            // Проверка портрета
            XElement format = new XElement("record", new XAttribute("type", "person"),
                    new XElement("field", new XAttribute("prop", "name")),
                    new XElement("field", new XAttribute("prop", "age")),
                    new XElement("inverse", new XAttribute("prop", "reflected"),
                        new XElement("record", new XAttribute("type", "reflection"),
                            new XElement("direct", new XAttribute("prop", "in_doc"),
                                new XElement("record", new XAttribute("type", "photo_doc"),
                                    new XElement("field", new XAttribute("prop", "name")))),
                            null)),
                    null);
            sw.Restart();
            XElement portrait = null;
            portrait = db.GetPortraitById(2870, format);
            sw.Stop();
            //Console.WriteLine(portrait.ToString());
            System.Console.WriteLine("GetPortraitById OK. duration={0}", sw.ElapsedMilliseconds); // 18 ms. 

            // Проверка серии портретов
            Random rnd = new Random(999999); int i = 0;
            sw.Restart();
            for (; i<100; i++) portrait = db.GetPortraitById(rnd.Next(10000), format);
            sw.Stop();
            //Console.WriteLine(portrait.ToString());
            System.Console.WriteLine("GetPortraitById OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 229 ms. 

            // Проверка той же серии портретов
            rnd = new Random(999999); i = 0;
            sw.Restart();
            for (; i < 100; i++) portrait = db.GetPortraitById(rnd.Next(10000), format);
            sw.Stop();
            //Console.WriteLine(portrait.ToString());
            System.Console.WriteLine("GetPortraitById OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 64 ms. 
        }
        private static string schema_str =
@"<schema>
  <record type='person'>
    <field prop='name' datatype='string' />
    <field prop='age' datatype='int' />
  </record>
  <record type='photo_doc'>
    <field prop='name' datatype='string' />
  </record>
  <record type='reflection'>
    <direct prop='reflected'><record type='person' /> </direct>
    <direct prop='in_doc'><record type='photo_doc' /> </direct>
  </record>
</schema>
";
    }
}
