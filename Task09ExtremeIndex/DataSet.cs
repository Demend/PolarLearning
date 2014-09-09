using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace Task09ExtremeIndex
{
    public class DataSet
    {
        private string path;
        private PType tp_persons, tp_photo_docs, tp_reflections, tp_index;
        private PaCell cell_persons, cell_photo_docs, cell_reflections;
        private ExtremeIndex<int> index_person_id, index_photo_doc_id, index_reflection_reflected, index_reflection_in_doc;
        public DataSet(string path)
        {
            this.path = path;
            tp_persons = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("age", new PType(PTypeEnumeration.integer))));
            tp_photo_docs = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));
            tp_reflections = new PTypeSequence(new PTypeRecord(
                new NamedType("reflected", new PType(PTypeEnumeration.integer)),
                new NamedType("in_doc", new PType(PTypeEnumeration.integer))));
            tp_index = new PTypeSequence(new PType(PTypeEnumeration.longinteger)); // Это по-старому...
            cell_persons = new PaCell(tp_persons, path + "persons.pac", false); if (cell_persons.IsEmpty) cell_persons.Fill(new object[0]);
            cell_photo_docs = new PaCell(tp_photo_docs, path + "photo_docs.pac", false); if (cell_photo_docs.IsEmpty) cell_photo_docs.Fill(new object[0]);
            cell_reflections = new PaCell(tp_reflections, path + "reflections.pac", false); if (cell_reflections.IsEmpty) cell_reflections.Fill(new object[0]);
            index_person_id = new ExtremeIndex<int>(path + "index_person_id.pac", cell_persons.Root, ent => (int)ent.Field(0).Get());
            index_photo_doc_id = new ExtremeIndex<int>(path + "index_photo_doc_id.pac", cell_photo_docs.Root, ent => (int)ent.Field(0).Get());
            index_reflection_reflected = new ExtremeIndex<int>(path + "index_reflection_reflected.pac", cell_reflections.Root, ent => 
                (int)ent.Field(0).Get());
            index_reflection_in_doc = new ExtremeIndex<int>(path + "index_reflection_in_doc.pac", cell_reflections.Root, ent =>
                (int)ent.Field(1).Get());
        }
        public void LoadXML(IEnumerable<XElement> element_flow)
        {
            // очистка коллекций
            cell_persons.Clear(); cell_persons.Fill(new object[0]);
            cell_photo_docs.Clear(); cell_photo_docs.Fill(new object[0]);
            cell_reflections.Clear(); cell_reflections.Fill(new object[0]);
            // Собственно загрузка
            foreach (XElement element in element_flow)
            {
                string type = element.Name.LocalName;
                string id = element.Attribute("id").Value;
                if (type == "person")
                {
                    string name = element.Element("name").Value;
                    int key = Int32.Parse(id);
                    int age = Int32.Parse(element.Element("age").Value);
                    cell_persons.Root.AppendElement(new object[] { key, name, age });
                }
                else if (type == "photo_doc")
                {
                    string name = element.Element("name").Value;
                    int key = Int32.Parse(id);
                    cell_persons.Root.AppendElement(new object[] { key, name });
                }
                else if (type == "reflection")
                {
                    int reflected = Int32.Parse(element.Element("reflected").Value);
                    int in_doc = Int32.Parse(element.Element("in_doc").Value);
                    cell_persons.Root.AppendElement(new object[] { reflected, in_doc });
                }
            }
            // Теперь для каждок коллекции надо сделать Flush()
            cell_persons.Flush();
            cell_photo_docs.Flush();
            cell_reflections.Flush();
            // Построение индексов
            index_person_id.Load();
            index_photo_doc_id.Load();
            index_reflection_reflected.Load();
            index_reflection_in_doc.Load();
        }
    }
}
