using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace Task10NameTable
{
    public class NameTable
    {
        private string path, csspath;
        private PType tp_csseq;
        private PaCell cssequence, offsets;
        public Func<int, string> GetStringByCode;
        public Func<string, int> GetCodeByString;
        public NameTable(string path)
        {
            this.path = path;
            tp_csseq = new PTypeSequence(new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PType(PTypeEnumeration.sstring))));
            csspath = path + "cssequence.pac";
            cssequence = new PaCell(tp_csseq, csspath, false);
            if (cssequence.IsEmpty) cssequence.Fill(new object[0]);
            offsets = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "offsets.pac", false);
            Func<int, string> GetStringByCode = (int code) => 
            {
                if (offsets.IsEmpty || code < 0 || code >= offsets.Root.Count()) throw new Exception("Unfilled data in NameTable");
                long off = (long)offsets.Root.Element(code).Get();
                if (cssequence.IsEmpty || cssequence.Root.Count() == 0) throw new Exception("Unfilled data in NameTable");
                PaEntry ent = cssequence.Root.Element(0);
                ent.offset = off;
                return (string)ent.Field(1).Get();
            };
        }
        public void Clear()
        {
            cssequence.Clear(); cssequence.Fill(new object[0]);
        }
        public void Show()
        {
            //PType tp = new PTypeRecord(
            //    new NamedType("code", new PType(PTypeEnumeration.integer)),
            //    new NamedType("string", new PType(PTypeEnumeration.sstring)));
            //foreach (var pv in cssequence.Root.ElementValues())
            //{
            //    Console.WriteLine(tp.Interpret(pv));
            //}
            Console.WriteLine("nelements={0}", cssequence.Root.Count());
        }
        public Dictionary<string, int> InsertPortion(string[] sorted_arr)
        {
            string[] ssa = sorted_arr;
            if (ssa.Length == 0) return new Dictionary<string, int>();
            
            cssequence.Close();
            // Подготовим основную ячейку для работы
            if (System.IO.File.Exists(path + "tmp.pac")) System.IO.File.Delete(path + "tmp.pac");
            System.IO.File.Move(csspath, path + "tmp.pac");

            PaCell source = new PaCell(tp_csseq, path + "tmp.pac");
            PaCell target = new PaCell(tp_csseq, csspath, false);
            target.Fill(new object[0]);

            int ssa_ind = 0;
            bool ssa_notempty = true;
            string ssa_current = ssa_notempty ? ssa[ssa_ind] : null;
            ssa_ind++;

            // Для накопления пар  
            List<KeyValuePair<string, int>> accumulator = new List<KeyValuePair<string, int>>(ssa.Length);

            // Очередной (новый) код (индекс)
            int code_new = 0;
            if (!source.IsEmpty)
            {
                code_new = (int)source.Root.Count();
                foreach (object[] val in source.Root.ElementValues())
                {
                    // Пропускаю элементы из нового потока, которые меньше текущего сканированного элемента 
                    string s = (string)val[1];
                    int cmp = 0;
                    while (ssa_notempty && (cmp = ssa_current.CompareTo(s)) <= 0)
                    {
                        if (cmp < 0)
                        { // добавляется новый код
                            object[] v = new object[] { code_new, ssa_current };
                            target.Root.AppendElement(v);
                            code_new++;
                            accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                        }
                        else
                        { // используется существующий код
                            accumulator.Add(new KeyValuePair<string, int>((string)val[1], (int)val[0]));
                        }
                        if (ssa_ind < ssa.Length)
                            ssa_current = ssa[ssa_ind++]; //ssa.ElementAt<string>(ssa_ind);
                        else
                            ssa_notempty = false;
                    }
                    target.Root.AppendElement(val); // переписывается тот же объект
                }
            }
            // В массиве ssa могут остаться элементы, их надо просто добавить
            if (ssa_notempty)
            {
                do
                {
                    object[] v = new object[] { code_new, ssa_current };
                    target.Root.AppendElement(v);
                    code_new++;
                    accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                    if (ssa_ind < ssa.Length) ssa_current = ssa[ssa_ind];
                    ssa_ind++;
                }
                while (ssa_ind <= ssa.Length);
            }

            target.Flush();
            source.Close();
            System.IO.File.Delete(path + "tmp.pac");
            cssequence = target;

            // Финальный аккорд: формирование и выдача словаря
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (var keyValuePair in accumulator.Where(keyValuePair => !dic.ContainsKey(keyValuePair.Key)))
            {
                dic.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return dic;
        }

    }
}
