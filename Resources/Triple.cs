using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.RDFSimple
{
    public abstract class Triple
    {
        public string subject, predicate;
    }
    public class OTriple : Triple
    {
        public string obj;
    }
    public class DTriple : Triple
    {
        public string data;
    }
}
