using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task09ExtremeIndex
{
    // Параметризованный класс, Tkey тип ключа, ключ должен быть упорядочиваемым через CompareTo
    public class ExtremeIndex
    {
        private PaEntry table;
        private PaCell index_cell;
        private int keyField;
        public ExtremeIndex(string indexName, PaEntry table, int keyField)
        {
            PType tp_index = new PTypeSequence(new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("value", new PType(PTypeEnumeration.integer))));
            this.table = table;
            this.keyField = keyField;
            index_cell = new PaCell(tp_index, indexName, false);
        }
        public void Close() { index_cell.Close(); }

        public void Load()
        {
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements()) // загрузка всех элементов за исключением уничтоженных
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(new object[] { offset, (int)rec.Field(keyField).Get() });
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            index_cell.Root.SortByKey<int>((object v) =>
            {
                var pair = (object[])v;
                return (int)pair[1];
            });
        }
        // Возвращает первый вход опорной таблицы, для которого сгенерированный ключ совпадает с образцом
        public PaEntry GetFirstByKey(int key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            PaEntry entry = table.Element(0);
            var candidate = index_cell.Root.BinarySearchFirst(ent =>
            {
                int val = (int)ent.Field(1).Get();
                return val.CompareTo(key);
            });
            if (candidate.IsEmpty) return PaEntry.Empty;
            entry.offset = (long)candidate.Field(0).Get();
            return entry;
        }
        // Возвращает множество входов в записи опорной таблицы, удовлетворяющие elementDepth == 0
        public IEnumerable<PaEntry> GetAll(int key)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            PaEntry entry = table.Element(0);
            Diapason dia = index_cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                int val = (int)ent.Field(1).Get();
                return val.CompareTo(key);
            });
            var query = index_cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Field(0).Get();
                    return entry;
                });
            return query;
        }
    }
}
