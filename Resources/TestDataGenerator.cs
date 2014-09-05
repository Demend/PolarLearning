using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Polar.Data
{
    public class TestDataGenerator
    {
        private int npersons, nphotos, nparticipations;
        private int seed;
        public TestDataGenerator(int npersons, int seed)
        {
            this.npersons = npersons;
            this.nphotos = npersons * 2;
            this.nparticipations = npersons * 6;
            this.seed = seed;
        }
        public IEnumerable<XElement> Generate()
        {
            Random rnd = new Random(seed);
            for (int i = 0; i < npersons; i++)
            {
                yield return new XElement("person", new XAttribute("id", i),
                    new XElement("name", "Пупкин" + rnd.Next(npersons)),
                    new XElement("age", 20 + rnd.Next(80)));
            }
            for (int i = 0; i < nphotos; i++)
            {
                yield return new XElement("photo_doc", new XAttribute("id", i),
                    new XElement("name", "DSP" + i));
            }
            for (int i = 0; i < nparticipations; i++)
            {
                yield return new XElement("reflection", new XAttribute("id", i),
                    new XElement("reflected", new XAttribute("ref", rnd.Next(npersons - 1))),
                    new XElement("in_doc", new XAttribute("ref", rnd.Next(nphotos - 1))));

            }
        }
    }
}
