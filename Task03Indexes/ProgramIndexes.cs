using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task03Indexes
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = "../../../Databases/";
            PType tp_seq_str = new PTypeSequence(new PType(PTypeEnumeration.sstring));
            PaCell scell = new PaCell(tp_seq_str, path + "seq_str.pac", false);
            scell.Clear();
            // Заполним последовательность строковыми значениями арифметической прогрессии
            scell.Fill(new object[0]);
            int snumber = 1000000;
            for (int i = 0; i < snumber; i++) scell.Root.AppendElement("" + i);
            scell.Flush();

            // Заведем ячейку для индекса
            PType tp_index = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
            PaCell index = new PaCell(tp_index, path + "index.pac", false);
            index.Clear();

            // Запишем значения в индекс
            index.Fill(new object[0]);
            foreach (PaEntry ent in scell.Root.Elements())
            {
                long offset = ent.offset;
                index.Root.AppendElement(offset);
            }
            index.Flush();

            // Сортируем индекс
            PaEntry entry = scell.Root.Element(0);
            index.Root.SortByKey<string>(ob_off =>
            {
                entry.offset = (long)ob_off;
                return (string)entry.Get();
            });

            // Поиск по значению
            string searchstring = "" + (snumber - 1);
            var en_found = index.Root.BinarySearchFirst(ent =>
            {
                entry.offset = (long)ent.Get();
                return ((string)entry.Get()).CompareTo(searchstring);
            });
            if (en_found.IsEmpty)
            {
                Console.WriteLine("Value " + searchstring + " was not found!");
            }
            else
            {
                entry.offset = (long)en_found.Get();
                Console.WriteLine("Value " + searchstring + " was found as " +
                    (string)entry.Get());
            }

            // ==== Вторая часть занятия ====
            // Создаем последовательность записей из трех полей, заполняем ее тестовыми данными
            PType tp_recs = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("age", new PType(PTypeEnumeration.integer))));
            PaCell records = new PaCell(tp_recs, path + "records.pac", false);
            records.Clear();
            records.Fill(new object[0]);
            Random rnd = new Random();
            int nrecs = 100000;
            for (int i = 0; i < nrecs; i++)
            {
                records.Root.AppendElement(new object[] { nrecs - i - 1, "Пупкин" + rnd.Next(nrecs), 20 + rnd.Next(20) });
            }
            records.Flush();

            // Добавим индексы
            Index<int> index_id = new Index<int>(path + "index_id.pac", records.Root, ent => (int)ent.Field(0).Get());
            Index<string> index_name = new Index<string>(path + "index_name.pac", records.Root, ent => (string)ent.Field(1).Get());
            Index<int> index_age = new Index<int>(path + "index_age.pac", records.Root, ent => (int)ent.Field(2).Get());
            // Загрузим индексы, поскольку в первый раз
            index_id.Load();
            index_name.Load();
            index_age.Load();

            // Произведем поиск (записей) по частичному совпадению образца с именем
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            
            string sample = "Пупкин8888";
            // Первый способ - перебор всех записей
            sw.Start();
            var query1 = records.Root.Elements()
                .Select(en => (object[])en.Get())
                .Where(ob => ((string)ob[1]).StartsWith(sample));
            foreach (var rec in query1) Console.WriteLine("{0} {1} {2}", rec[0], rec[1], rec[2]);
            sw.Stop();
            Console.WriteLine("duration={0}", sw.ElapsedMilliseconds); // 1945
            // Второй способ - используя индекс
            sw.Restart();
            var query2 = index_name.GetAll(en =>
            {
                string name = (string)en.Field(1).Get();
                if (name.StartsWith(sample)) return 0;
                return name.CompareTo(sample);
            }).Select(ent => (object[])ent.Get());
            foreach (var rec in query2) Console.WriteLine("{0} {1} {2}", rec[0], rec[1], rec[2]);
            sw.Stop();
            Console.WriteLine("duration={0}", sw.ElapsedMilliseconds); // 9
        }
    }
}
