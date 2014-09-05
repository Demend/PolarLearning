using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Polar.RDFSimple;

namespace Task08SimpleGraph
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("SimpleGraph starts.");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            SimpleGraph sg = new SimpleGraph("../../../Databases/");
            int npersons = 10000;
            bool toload = true;
            if (toload)
            {
                sw.Restart();
                var tg = new Polar.Data.TestDataGenerator(npersons, 777777);
                var query = tg.Generate()
                    .SelectMany(el =>
                    {
                        string id = el.Name.LocalName + el.Attribute("id").Value;
                        return new Triple[] { new OTriple() { subject = id, predicate = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", obj = el.Name.LocalName } }
                            .Concat(el.Elements()
                                .Select<XElement, Triple>(sub_el =>
                                {
                                    XAttribute ref_att = sub_el.Attribute("ref");
                                    if (ref_att == null) return new DTriple() { subject = id, predicate = sub_el.Name.LocalName, data = sub_el.Value };
                                    else return new OTriple()
                                    {
                                        subject = id,
                                        predicate = sub_el.Name.LocalName,
                                        obj = (sub_el.Name == "reflected" ? "person" : "photo_doc") + ref_att.Value
                                    };
                                }));
                    });
                sg.Load(query);
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds);
            }
            else
            {
                // Разогрев
                sw.Restart();
                sg.Warmup();
                sw.Stop();
                Console.WriteLine("Warmup ok. Duration={0}", sw.ElapsedMilliseconds);
            }

            sw.Restart();
            XElement res = sg.GetSimplePortrait("person2870");
            sw.Stop();
            Console.WriteLine(res.ToString());
            Console.WriteLine("GetSimplePortrait ok. Duration={0}", sw.ElapsedMilliseconds);

            XElement format = new XElement("record", new XAttribute("type", "person"),
                new XElement("field", new XAttribute("prop", "name")),
                new XElement("field", new XAttribute("prop", "age")),
                new XElement("inverse", new XAttribute("prop", "reflected"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "in_doc"),
                            new XElement("record", new XAttribute("type", "photo_doc"),
                                new XElement("field", new XAttribute("prop", "name")))))),
                null);
            sw.Restart();
            res = sg.GetPortrait("person2870", format);
            sw.Stop();
            Console.WriteLine(res.ToString());
            Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds);

            // Проверка серии портретов
            Random rnd = new Random(999999); int i = 0;
            sw.Restart();
            for (; i < 100; i++) sg.GetPortrait("person"+rnd.Next(10000), format);
            sw.Stop();
            System.Console.WriteLine("GetPortrait OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 1760 ms. 

            // Проверка серии портретов AGAIN
            rnd = new Random(); i = 0;
            sw.Restart();
            for (; i < 100; i++) sg.GetPortrait("person" + rnd.Next(10000), format);
            sw.Stop();
            System.Console.WriteLine("GetPortrait OK. times={0} duration={1}", i, sw.ElapsedMilliseconds); // 1760 ms. 
        }
    }
}
