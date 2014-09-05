using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task05FlexIndex
{
    class SQLtest
    {
        public SQLtest()
        {
            string connectionstring = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\Александр\Documents\test20140823.mdf;Integrated Security=True;Connect Timeout=30";
            string dataprovider = "System.Data.SqlClient";
            DbProviderFactory factory = DbProviderFactories.GetFactory(dataprovider);
            DbConnection connection = factory.CreateConnection();
            connection.ConnectionString = connectionstring;
            DbCommand comm;

            bool firsttime = true;
            if (firsttime)
            {
                connection.Open();
                comm = connection.CreateCommand();
                comm.CommandText = "DROP TABLE persons;";
                string message = null;
                try { comm.ExecuteNonQuery(); }
                catch (Exception ex) { message = ex.Message; }
                comm.CommandText =
    @"CREATE TABLE persons (name NVARCHAR(400), age INT NOT NULL);";
                try { comm.ExecuteNonQuery(); }
                catch (Exception ex) { message = ex.Message; }
                connection.Close();
                if (message != null) Console.WriteLine(message);

                connection.Open();
                comm = connection.CreateCommand();
                comm.CommandTimeout = 2000;
                comm.CommandText =
    @"CREATE INDEX person_name ON persons(name);";
                try
                {
                    comm.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                connection.Close();
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random(88888);
            sw.Restart();
            int i;
            for (i = 0; i < 1000; i++)
            {
                connection.Open();
                comm = connection.CreateCommand();
                comm.CommandText = "INSERT INTO persons VALUES (N'Пупкин" + rnd.Next(999999) + "', " + 20 + rnd.Next(19) + ");";
                comm.ExecuteNonQuery();
                connection.Close();
            }
            sw.Stop();
            Console.WriteLine("К-во записей: {0} время загрузки: {1}", i, sw.ElapsedMilliseconds); //100: 187 мс.  1000: 1183 мс, 10000: 10-14 сек.

            // Пробный запрос
            sw.Restart();
            connection.Open();
            comm = connection.CreateCommand();
            comm.CommandText = "SELECT COUNT(*) FROM persons WHERE name LIKE N'Пупкин9%';";
            object ob = comm.ExecuteScalar();
            connection.Close();
            sw.Stop();
            Console.WriteLine("count()={0} duration={1}", ob, sw.ElapsedMilliseconds); // count()=100 duration=17
        }
    }
}
