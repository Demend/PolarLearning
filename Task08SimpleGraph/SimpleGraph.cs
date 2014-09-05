using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using Polar.Indexes;
using Polar.RDFSimple;

namespace Task08SimpleGraph
{
    public class SimpleGraph
    {
        private string path;
        private PaCell otriples, dtriples;
        private IndexView<string> s_index, o_index, q_index;
        public SimpleGraph(string path)
        {
            this.path = path;
            PType tp_otriples = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("obj", new PType(PTypeEnumeration.sstring))));
            PType tp_dtriples = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("data", new PType(PTypeEnumeration.sstring))));

            otriples = new PaCell(tp_otriples, path + "otriples.pac", false);
            if (otriples.IsEmpty) otriples.Fill(new object[0]);
            dtriples = new PaCell(tp_dtriples, path + "dtriples.pac", false);
            if (dtriples.IsEmpty) dtriples.Fill(new object[0]);
            s_index = new IndexView<string>(path + "s_index.pac", 
                otriples.Root, en => (string)en.Field(0).Get());
            o_index = new IndexView<string>(path + "o_index.pac",
                otriples.Root, en => (string)en.Field(2).Get());
            q_index = new IndexView<string>(path + "q_index.pac",
                dtriples.Root, en => (string)en.Field(0).Get());
        }
        public void Load(IEnumerable<Polar.RDFSimple.Triple> triples)
        {
            otriples.Clear(); otriples.Fill(new object[0]);
            dtriples.Clear(); dtriples.Fill(new object[0]);

            foreach (var triple in triples)
            {
                if (triple is OTriple)
                {
                    otriples.Root.AppendElement(new object[] { triple.subject, triple.predicate, ((OTriple)triple).obj });
                }
                else if (triple is DTriple)
                {
                    dtriples.Root.AppendElement(new object[] { triple.subject, triple.predicate, ((DTriple)triple).data });
                }
                else throw new Exception("Assert exception: 29482");
            }
            otriples.Flush();
            dtriples.Flush();

            s_index.Load(null);
            o_index.Load(null);
            q_index.Load(null);
        }
        // Разогрев
        internal void Warmup()
        {
            foreach (var v in otriples.Root.ElementValues()) ;
            foreach (var v in dtriples.Root.ElementValues()) ;
            s_index.Warmup();
            o_index.Warmup();
            q_index.Warmup();
        }
        // Простой портрет
        public XElement GetSimplePortrait(string id)
        {
            XElement res = new XElement("record", new XAttribute("id", id));
            var query1 = s_index.GetAllByKey(id);
            string type = null;
            foreach (var entry in query1)
            {
                object[] pvalue = (object[])entry.Get();
                string pred = (string)pvalue[1];
                string obj = (string)pvalue[2];
                if (pred == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type") type = obj;
                else
                {
                    res.Add(new XElement("direct", new XAttribute("prop", pred),
                        new XElement("record", new XAttribute("id", obj))));
                }
            }

            if (type == null) return null; // Если тип не определен, то ...
            res.Add(new XAttribute("type", type));

            var query2 = q_index.GetAllByKey(id);
            foreach (var entry in query2)
            {
                object[] pvalue = (object[])entry.Get();
                string pred = (string)pvalue[1];
                string data = (string)pvalue[2];
                res.Add(new XElement("field", new XAttribute("prop", pred), data));
            }

            return res;
        }
        // Сложный портрет
        public XElement GetPortrait(string id, XElement format)
        {
            XElement res = new XElement("record", new XAttribute("id", id));
            var pred_obj_pairs = s_index.GetAllByKey(id)
                .Select(ent => 
                {
                    object[] pvalue = (object[])ent.Get();
                    string pred = (string)pvalue[1];
                    string obj = (string)pvalue[2];
                    return new { pred = pred, obj = obj };
                }).ToArray();
            var type_pair = pred_obj_pairs.FirstOrDefault(pair => pair.pred == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
            if (type_pair == null || (format.Attribute("type") != null && type_pair.obj != format.Attribute("type").Value)) return null;
            res.Add(new XAttribute("type", type_pair.obj));

            var pred_data_pairs = q_index.GetAllByKey(id)
                .Select(ent =>
                {
                    object[] pvalue = (object[])ent.Get();
                    string pred = (string)pvalue[1];
                    string data = (string)pvalue[2];
                    return new { pred = pred, data = data };
                }).ToArray();
            var pred_subj_pairs = o_index.GetAllByKey(id)
                .Select(ent =>
                {
                    object[] pvalue = (object[])ent.Get();
                    string pred = (string)pvalue[1];
                    string subj = (string)pvalue[0];
                    return new { pred = pred, subj = subj };
                }).ToArray();


            foreach (XElement fel in format.Elements())
            {
                string prop = fel.Attribute("prop").Value;
                if (fel.Name == "field")
                {
                    var q = pred_data_pairs.FirstOrDefault(pair => pair.pred == prop);
                    if (q != null) res.Add(new XElement("field", new XAttribute("prop", prop), q.data));
                }
                else if (fel.Name == "direct")
                {
                    var q = pred_obj_pairs.FirstOrDefault(pair => pair.pred == prop);
                    if (q != null) res.Add(new XElement("direct", new XAttribute("prop", prop), 
                        GetPortrait(q.obj, fel.Element("record"))));
                }
                else if (fel.Name == "inverse")
                {
                    foreach (var pair in pred_subj_pairs.Where(pr => pr.pred == prop))
                    {
                        res.Add(new XElement("inverse", new XAttribute("prop", prop),
                            GetPortrait(pair.subj, fel.Element("record"))));
                    }
                }
                else throw new Exception("Assert err: 29283");
            }

            return res;
        }
    }
}
