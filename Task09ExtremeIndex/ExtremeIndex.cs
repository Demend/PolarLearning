using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task09ExtremeIndex
{
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
            if (opt3) Opt3Init(indexName);
        }
        //public void Close() { index_cell.Close(); }

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
            if (opt3) Opt3Load();
        }
        public void Warmup()
        {
            foreach (var v in index_cell.Root.ElementValues()) ;
            if (opt3) Opt3Warmup();
        }
        // Методом аппроксимации, находит первый вход опорной таблицы, для которого сгенерированный ключ совпадает с образцом
        public PaEntry GetFirstByKey(int key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            PaEntry entry = table.Element(0);
            if (key < 0 || key >= index_cell.Root.Count()) throw new Exception("Out of range");
            PaEntry candidate = index_cell.Root.Element(key);
            object[] pair = (object[])candidate.Get();
            if ((int)pair[1] != key) throw new Exception("Assert err: 29991");
            entry.offset = (long)pair[0];
            return entry;
        }
        // Возвращает первый вход опорной таблицы, для которого сгенерированный ключ совпадает с образцом (старый вариант)
        public PaEntry GetFirstByKey0(int key)
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
        public IEnumerable<PaEntry> GetAll(int key)
        {
            if (opt3) return GetAllopt3(key);
            else return GetAll0(key);
        }

        // Возвращает множество входов в записи опорной таблицы, имеющие данный ключ (старый вариант)
        private IEnumerable<PaEntry> GetAll0(int key)
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
        // ===== Метод 3 =====
        private bool opt3 = false;
        // Дополнительная таблица
        PaCell additional_index;
        private void Opt3Init(string indexName)
        {
            PType tp_add = new PTypeSequence(new PTypeRecord(
                new NamedType("key", new PType(PTypeEnumeration.integer)),
                new NamedType("diap", new PTypeRecord(
                    new NamedType("start", new PType(PTypeEnumeration.longinteger)),
                    new NamedType("number", new PType(PTypeEnumeration.longinteger))))));
            additional_index = new PaCell(tp_add, indexName.Replace(".pac", "_add.pac"), false);
        }
        private void Opt3Load()
        {
            additional_index.Clear();
            additional_index.Fill(new object[0]);
            int counter = -1; // Счетчик, который фиксирует текущий индекс основной ячейки
            int key = -1; // Ключ на предыдущей итерации
            long start = 0; // начало текущей серии, с одинаковым ключем
            long number = 0; // зафиксированное количество
            foreach (object[] pair in index_cell.Root.ElementValues())
            {
                counter++;
                int key2 = (int)pair[1];
                // Или ключ новый или ключ продолжается
                if (key2 == key)
                {
                    number++;
                }
                else if (key2 > key)
                {
                    // Зафиксировать предыдущую серию
                    if (key != -1)
                    {
                        if (key != additional_index.Root.Count()) throw new Exception("assert 29382");
                        additional_index.Root.AppendElement(new object[] { key, new object[] { start, number } });
                    }
                    for (key = key + 1; key < key2; key++) // запишем предыдущие значения
                    {
                        additional_index.Root.AppendElement(new object[] { key, new object[] { start, 0L } });
                    } 
                    // После цикла, key == key2; и это значение не зафиксировано. Началась серия
                    start = counter; number = 1;
                }
                else throw new Exception("Assert err: 2499776"); // означает что ключ уменьшился
            }
            // Обязательно есть еще не зафиксированная серия. Зафиксируем ее
            if (key != additional_index.Root.Count()) throw new Exception("assert 29382");
            additional_index.Root.AppendElement(new object[] { key, new object[] { start, number } });
            additional_index.Flush();
        }
        private void Opt3Warmup()
        {
            foreach (var v in additional_index.Root.ElementValues()) ;
        }
        public IEnumerable<PaEntry> GetAllopt3(int key)
        {
            if (table.Count() == 0 || key >= additional_index.Root.Count()) return Enumerable.Empty<PaEntry>();

            object[] pair = (object[])additional_index.Root.Element(key).Get();
            int code = (int)pair[0];
            long start = (long)((object[])pair[1])[0];
            long number = (long)((object[])pair[1])[1];

            PaEntry entry = table.Element(0);
            var query = index_cell.Root.Elements(start, number)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Field(0).Get();
                    return entry;
                });
            return query;
        }
    }
}
