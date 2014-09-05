using System;
using PolarDB;

namespace Task01HelloWorld
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string path = "../../../Databases/";
            PType tp = new PType(PTypeEnumeration.sstring);
            PaCell cell = new PaCell(tp, path + "test.pac", false);
            cell.Clear();
            cell.Fill("Привет из ячейки базы данных!");
            Console.WriteLine("Содержимое ячейки: {0}", cell.Root.Get());

            PType tp_rec = new PTypeRecord(
                new NamedType("имя", new PType(PTypeEnumeration.sstring)),
                new NamedType("возраст", new PType(PTypeEnumeration.integer)),
                new NamedType("мужчина", new PType(PTypeEnumeration.boolean)));
            object rec_value = new object[] { "Пупкин", 16, true };
            PaCell cell_rec = new PaCell(tp_rec, path + "test_rec.pac", false);
            cell_rec.Clear();
            cell_rec.Fill(rec_value);
            object from_rec = cell_rec.Root.Get();
            Console.WriteLine(tp_rec.Interpret(from_rec));

            // Пары: тип, значение данного типа
            PType tp_seq_int = new PTypeSequence(new PType(PTypeEnumeration.integer));
            object seq_int_value = new object[] { 1, 2, 3, 4, 5, 6, 7 };
 
            PType tp_seq_rec = new PTypeSequence(new PTypeRecord(
                new NamedType("имя", new PType(PTypeEnumeration.sstring)),
                new NamedType("возраст", new PType(PTypeEnumeration.integer)),
                new NamedType("мужчина", new PType(PTypeEnumeration.boolean))));
            object seq_rec_value = new object[] {
                new object[] { "Пупкин", 16, true },
                new object[] { "Иванов", 61, true },
                new object[] { "Петрова", 33, false }
            };
        
            PType tp_point = new PTypeRecord(
                new NamedType("X", new PType(PTypeEnumeration.real)),
                new NamedType("Y", new PType(PTypeEnumeration.real)));
            PType tp_figure = new PTypeUnion(
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("point", tp_point),
                new NamedType("line", new PTypeRecord(new NamedType("p1", tp_point), new NamedType("p2", tp_point))),
                new NamedType("circle", new PTypeRecord(new NamedType("p", tp_point), new NamedType("r", new PType(PTypeEnumeration.real)))));
            object figure_value = new object[] { 1, new object[] { 1.3, 0.777 } }; // Точка

            PType tp_intnullable = new PTypeUnion(
                new NamedType("null", new PType(PTypeEnumeration.none)),
                new NamedType("ival", new PType(PTypeEnumeration.integer)));
            object vnull = new object[] { 0, null };
            object vint = new object[] { 1, -777 };
        }
    }
}
