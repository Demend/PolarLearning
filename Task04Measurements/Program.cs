using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task04Measurements
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = "../../../Databases/";
            Console.WriteLine("Start.");

            // Заводим большую последовательность целых
            PType tp_seq_int = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
            PaCell cell = new PaCell(tp_seq_int, path + "cell.pac", false);

            // Загрузка
            int nnumbers = 10000000;
            bool toload = true;
            if (toload)
            {
                cell.Clear();
                cell.Fill(new object[0]);
                for (int i = 0; i < nnumbers; i++) cell.Root.AppendElement((long)(i + 1));
                cell.Flush();
            }

            Random rnd = new Random();
            int portion = 1000;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            // Разогрев
            bool towarmup = false;
            if (towarmup)
            {
                sw.Restart();
                foreach (var v in cell.Root.ElementValues()) ;
                sw.Stop();
                Console.WriteLine("Warmup. duration={0}", sw.ElapsedMilliseconds);
            }

            // Случайное чтение
            sw.Restart();
            for (int i = 0; i < portion; i++)
            {
                var val = cell.Root.Element(rnd.Next(nnumbers - 1)).Get();
            }
            sw.Stop();
            Console.WriteLine("Random access. portion={0} duration={1}", portion, sw.ElapsedMilliseconds);

            rnd = new Random();
            portion = 1000000;
            long[] arr = Enumerable.Range(1, nnumbers).Select(n => (long)n).ToArray();
            int[] indexes = Enumerable.Range(1, portion).Select(n => rnd.Next(nnumbers - 1)).ToArray();
            sw.Restart();
            for (int i = 0; i < portion; i++)
            {
                var val = arr[indexes[i]];
            }
            sw.Stop();
            Console.WriteLine("Random access. portion={0} duration={1}", portion, sw.ElapsedMilliseconds);
        }
    }
}
