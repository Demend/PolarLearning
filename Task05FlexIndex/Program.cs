using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using Polar.Indexes;

namespace Task05FlexIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start FlexIndex");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            string path = "../../../Databases/";

            //SQLtest test = new SQLtest();
            //return;

            // Зададим тип
            PType tp_rec = new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("age", new PType(PTypeEnumeration.integer)));
            PType tp_seq = new PTypeSequence(tp_rec);
            // Создадим (опорную) таблицу
            PaCell table = new PaCell(tp_seq, path + "table.pac", false);
            if (table.IsEmpty) table.Fill(new object[0]);

            // Создадим индекс на поле "name"
            FlexIndex<string> index_name = new FlexIndex<string>(path + "index_name", table.Root,
                ent => (string)ent.Field(1).Get(), null);

            // Почистим перед заполнением
            table.Clear(); table.Fill(new object[0]);
            index_name.Load();
            // Будем добавлять записи в таблицу, каждый раз "предъявляя" запись индексу
            Random rnd = new Random(88888);
            sw.Restart();
            int i;
            for (i = 0; i < 100; i++)
            {
                var offset = table.Root.AppendElement(new object[] { false, "Пупкин" + rnd.Next(999999), 20 + rnd.Next(19) });
                // Следующие две строки надо закомментарить для проверки Load()
                table.Flush();
                index_name.AddEntry(new PaEntry(tp_rec, offset, table));
            }
            // Следующие две строки надо разкомментарить для проверки Load()
            //table.Flush();
            //index_name.Load();
            sw.Stop();
            Console.WriteLine("К-во записей: {0} время загрузки: {1}", i, sw.ElapsedMilliseconds); // для 100: 65 мс., для 1000: 5.4-5.6 сек.

            // Пробный запрос
            sw.Restart();
            string searchstring = "Пупкин9";
            var query = index_name.GetAll(ent =>
            {
                string s = (string)ent.Field(1).Get();
                if (s.StartsWith(searchstring)) return 0;
                else return s.CompareTo(searchstring);
            });
            int count = query.Count();
            sw.Stop();
            Console.WriteLine("count()={0} duration={1}", count, sw.ElapsedMilliseconds); // count()=100 duration=11

        }
    }
}
