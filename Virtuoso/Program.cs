using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Polar.RDFSimple;

namespace VirtuosoTest
{
    public class Program
    {
        public static void Main()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            AdapterVirtuoso engine = new AdapterVirtuoso("HOST=localhost:1550;UID=dba;PWD=dba;Charset=UTF-8", "http://fogid.net/");

            //var val = engine.ExecuteScalar("SPARQL SELECT count(*) FROM <berlin100m> {?s ?p ?o}");
            //Console.WriteLine("Result = [{0}]", val);

            int npersons = 1000000;
            bool toload = false;

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
                engine.Load(query);
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds);
            }

            Random rnd = new Random();

            for (int j = 0; j < 10; j++)
            {
                sw.Restart();
                var runcommand = engine.RunStart();
                for (int i=0; i<1000; i++) 
                {
                    engine.GetReflections(rnd.Next(npersons - 1), runcommand).Count();
                }
                engine.RunStop(runcommand);
                sw.Stop();
                Console.WriteLine("GetReflections ok. Duration={0}", sw.ElapsedMilliseconds);
            }

        }


    }
}
