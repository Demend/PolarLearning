using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task02Sequences
{
    public class ProgramSequences
    {
        public static void Main(string[] args)
        {
            string path = "../../../Databases/";
            PType tp_seq_int = new PTypeSequence(new PType(PTypeEnumeration.integer));
            PaCell cell = new PaCell(tp_seq_int, path + "test_seq_int.pac", false);
            cell.Clear();
            cell.Fill(new object[] { 1, 2, 3, 4, 5, 6, 7 });
            Console.WriteLine(tp_seq_int.Interpret(cell.Root.Get()));

            cell.Root.AppendElement(8);
            cell.Root.AppendElement(9);
            cell.Root.AppendElement(10);
            cell.Flush();
            Console.WriteLine(tp_seq_int.Interpret(cell.Root.Get()));
            //return;
            
            int nvalues = 1000000;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch(); sw.Start();
            try
            {
                cell.Clear();
                var arr = Enumerable.Range(1, nvalues).Cast<object>().ToArray();
                cell.Fill(arr);
                sw.Stop(); Console.WriteLine("Fill by {0} elements. Duration={1} ms.", nvalues, sw.ElapsedMilliseconds); sw.Reset(); sw.Start();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { sw.Stop(); sw.Reset(); sw.Start(); }

            cell.Clear(); cell.Fill(new object[0]);
            for (int i = 0; i < nvalues; i++) cell.Root.AppendElement(i + 1);
            cell.Flush();
            // 1 mln. - 170 ms., 10 mln. - 1.1 s., 100 mln. - 29.5 s.
            sw.Stop(); Console.WriteLine("Append {0} elements. Duration={1} ms.", nvalues, sw.ElapsedMilliseconds); sw.Reset();

            // Читаем i-ый элемент и выводим
            int ind = 500000;
            int val = (int)cell.Root.Element(ind).Get();
            Console.WriteLine("Элемент № {0} = {1}", ind, val);

            // Подсчитываем сумму элементов
            long sum = 0L;
            foreach (var ele in cell.Root.Elements()) sum += (int)ele.Get();
            Console.WriteLine("Сумма элементов: {0}", sum);
            // Те, кто помнит как подсчитать сумму арифметической прогрессии, убедятся, что результат правильный...

            cell.Root.Element(ind).Set(0);
            cell.Root.SortByKey<int>(ob => (int)ob);
            Console.WriteLine("Элемент № {0} = {1}, № {2} = {3}", ind, (int)cell.Root.Element(ind).Get(), ind + 1, (int)cell.Root.Element(ind+1).Get());
            int searchvalue = 500000; // 500001;
            var found = cell.Root.BinarySearchFirst(ent => ((int)ent.Get()).CompareTo(searchvalue));
            Console.WriteLine("Value " + searchvalue + " " + (found.IsEmpty ? "not " : "") + "found!"); 

            Console.WriteLine("\n ==== Новый раздел - работа с последовательностью строк ====");
            // Заведем ячейку
            PType tp_seq_str = new PTypeSequence(new PType(PTypeEnumeration.sstring));
            PaCell scell = new PaCell(tp_seq_str, path + "seq_str.pac", false);
            scell.Clear();
            // Заполним последовательность строковыми значениями арифметической прогрессии
            scell.Fill(new object[0]);
            int snumber = 1000000;
            for (int i = 0; i < snumber; i++) scell.Root.AppendElement("" + i);
            scell.Flush();
            // Выведем первые 20
            var query = scell.Root.Elements().Select(en => (string)en.Get()).Take(20);
            foreach (var sval in query) Console.Write(sval + " ");
            Console.WriteLine();

            // Измерение времени доступа к первому и последнему элементам последовательности
            sw.Start();
            string s0 = (string)scell.Root.Element(0).Get();
            sw.Stop(); long t0 = sw.ElapsedMilliseconds; sw.Restart();
            string s1 = (string)scell.Root.Element(snumber / 2).Get();
            sw.Stop(); long t1 = sw.ElapsedMilliseconds; sw.Restart();
            string s2 = (string)scell.Root.Element(snumber - 1).Get();
            sw.Stop(); long t2 = sw.ElapsedMilliseconds;
            Console.WriteLine("values: {0} {1} {2}. times: {3} {4} {5} ms.", s0, s1, s2, t0, t1, t2);
        }
    }
}
