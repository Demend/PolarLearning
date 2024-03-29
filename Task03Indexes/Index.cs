﻿using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace Task03Indexes
{
    // Параметризованный класс, Tkey тип ключа, ключ должен быть упорядочиваемым через CompareTo
    public class Index<Tkey> where Tkey : IComparable
    {
        private PaEntry table;
        private PaCell index_cell;
        private Func<PaEntry, Tkey> keyProducer;
        public Index(string indexName, PaEntry table, Func<PaEntry, Tkey> keyProducer)
        {
            this.table = table;
            this.keyProducer = keyProducer;
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName, false);
            //if (index_cell.IsEmpty) index_cell.Fill(new object[0]);
        }
        public void Close() { index_cell.Close(); }

        public void Load()
        {
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements()) // загрузка всех элементов за исключением уничтоженных
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(offset);
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = table.Element(0);
            //
            index_cell.Root.SortByKey<Tkey>((object v) =>
            {
                ptr.offset = (long)v;
                return keyProducer(ptr);
            });
        }
        // Возвращает первый вход опорной таблицы, для которого сгенерированный ключ совпадает с образцом
        public PaEntry GetFirstByKey(Tkey key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            PaEntry entry = table.Element(0);
            var candidate = index_cell.Root.BinarySearchFirst(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            });
            if (candidate.IsEmpty) return PaEntry.Empty;
            entry.offset = (long)candidate.Get();
            return entry;
        }
        // Возвращает множество входов в записи опорной таблицы, удовлетворяющие elementDepth == 0
        public IEnumerable<PaEntry> GetAll(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            PaEntry entry = table.Element(0);
            Diapason dia = index_cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            });
            var query = index_cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                });
            return query;
        }
    }
}
