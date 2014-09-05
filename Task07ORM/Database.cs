using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using PolarDB;
using Polar.Indexes;

namespace Task07ORM
{
    class Database
    {
         // директория для базы данных
        private string path;
        public string Path { get { return path; } }
        private XElement schema;
        // Словарь коллекций
        internal Dictionary<string, Collection> collections = new Dictionary<string, Collection>(); 
        internal List<IndexContext> external_indexes = new List<IndexContext>();
        // Конструктор
        public Database(string path, XElement schema)
        {
            this.path = path;
            this.schema = schema;
            // Формирования поля ячеек
            foreach (XElement frecord in schema.Elements("record"))
            {
                string ftype = frecord.Attribute("type").Value;
                // Заводим коллекцию
                Collection collection = new Collection(ftype, schema, this);
                collections.Add(ftype, collection);
            }
        }
        public void LoadXML(IEnumerable<XElement> element_flow)
        {
            // очистка коллекций
            foreach (var coll in collections) coll.Value.Clear();
            // Собственно загрузка
            foreach (XElement element in element_flow)
            {
                string type = element.Name.LocalName;
                Collection collection = null;
                if (!collections.TryGetValue(type, out collection)) continue;
                string id = element.Attribute("id").Value;
                var pvalue = collection.FRecord.Elements()
                    .Where(el => el.Name == "field" || el.Name == "direct")
                    .Select(el =>
                    {
                        XElement sub = element.Element(el.Attribute("prop").Value);
                        object res = null;
                        if (el.Name == "direct")
                        {
                            res = Int32.Parse(sub.Attribute("ref").Value);
                        }
                        else
                        {
                            //Надо разобрать по типам
                            string dt = el.Attribute("datatype").Value;
                            if (dt == "string") res = sub.Value;
                            else if (dt == "int") res = Int32.Parse(sub.Value);
                            else res = sub.Value;
                        }
                        return res;
                    }).ToArray();
                collection.AppendElement(Int32.Parse(id), pvalue);
            }
            // Теперь для каждок коллекции надо сделать Flush()
            foreach (XElement frecord in schema.Elements("record"))
            {
                string ftype = frecord.Attribute("type").Value;
                Collection collection = collections[ftype];
                collection.Flush();
            }
        }
        // Поиск по имени
        public IEnumerable<XElement> SearchByNameIn(string searchstring, string type)
        {
            Collection collection = collections[type];
            string ss = searchstring.ToLower();
            return collection.Elements()
                .Where(el => ((string)((object[])el.Get())[0]).ToLower().StartsWith(ss))
                .Select(el => new XElement("record", new XAttribute("id", el.Key),
                    new XElement("name", (string)((object[])el.Get())[0])));
        }
        public XElement GetPortraitByIdIn(int key, string type)
        {
            Collection collection = collections[type];
            var pelement = collection.Element(key);
            if (pelement == null) return null;
            return new XElement("record", new XElement(type, new XAttribute("id", key), new XAttribute("type", type),
                new XElement("name", (string)((object[])pelement.Get())[0])));
        }

        public XElement GetPortraitById(int key, XElement format)
        {
            string type = format.Attribute("type").Value;
            Collection collection = collections[type];
            return GetPortraitById(collection.GetEntryByKey(key), format);
        }
        private XElement GetPortraitById(PaEntry ent, XElement format)
        {
            if (ent.IsEmpty) return null;
            string type = format.Attribute("type").Value;
            Collection collection = collections[type];
            object[] three = (object[])ent.Get();
            int key = (int)three[1];
            object[] pvalues = (object[])three[2];
            XElement[] fels = format.Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            XElement[] schem = collection.FRecord //schema.Elements("record").First(re => re.Attribute("type").Value == type)
                .Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            // Элементы pvalues по количеству и по сути соответствуют определениям schem
            if (pvalues.Length != schem.Length) throw new Exception("Assert Error 9843");

            XElement result = new XElement("record", new XAttribute("id", key), new XAttribute("type", type));
            var fields_directs = fels.Select(fd =>
            {
                string prop = fd.Attribute("prop").Value;
                int ind = FirstProp(schem, prop);
                XElement sch_el = schem[ind];
                XElement res = null;
                if (sch_el.Name == "field") res = new XElement("field", new XAttribute("prop", prop), pvalues[ind]);
                else if (sch_el.Name == "direct")
                {
                    int forward_key = (int)pvalues[ind];
                    res = new XElement("direct", new XAttribute("prop", prop), GetPortraitById(forward_key, fd.Element("record")));
                }
                return res;
            });
            result.Add(fields_directs);
            XElement[] iels = format.Elements("inverse").ToArray();
            foreach (var inv in format.Elements("inverse"))
            {
                string iprop = inv.Attribute("prop").Value;
                XElement rec = inv.Element("record");
                string itype = rec.Attribute("type").Value;
                var inde = external_indexes.FirstOrDefault(context => context.totype == type && context.prop == iprop && context.type == itype);
                if (inde == null) continue;
                foreach (PaEntry en in ((FlexIndex<int>)inde.index).GetAllByKey(key))
                {
                    //int ccod = (int)en.Field(1).Get();
                    result.Add(new XElement("inverse", new XAttribute("prop", iprop), GetPortraitById(en, rec)));
                }
            }

            return result;
        }
        private static int FirstProp(XElement[] sch, string prop)
        {
            return sch.Select((fd, ind) => new { fd = fd, ind = ind })
                .First(pair => pair.fd.Attribute("prop").Value == prop)
                .ind;
        }
   }
}
