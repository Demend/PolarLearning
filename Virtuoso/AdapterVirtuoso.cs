using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenLink.Data.Virtuoso;

namespace VirtuosoTest
{
    public class AdapterVirtuoso
    {
        private string graph = "http://fogid.net/t/";

        private VirtuosoConnection connection = null;
        public AdapterVirtuoso(string connectionstring, string graph)
        {
            this.connection = new OpenLink.Data.Virtuoso.VirtuosoConnection(connectionstring);
            this.graph = graph;
        }
        public int Execute(string sparql)
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            var command = connection.CreateCommand();
            //var db = connection.Database;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = sparql;
            int val = command.ExecuteNonQuery();
            connection.Close();
            return val;
        }
        public object ExecuteScalar(string sparql)
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = sparql;
            object val = command.ExecuteScalar();
            connection.Close();
            return val;
        }
        public IEnumerable<object[]> Query(string sparql)
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = sparql;
            var reader = command.ExecuteReader();

            int ncols = reader.FieldCount;
            object[] data = new object[ncols];
            while (reader.Read())
            {
                reader.GetValues(data);
                yield return data;
            }
            reader.Close();
            connection.Close();
        }
        // Начальная и конечная "скобки" транзакции. В серединке должны использоваться команды ТОЛЬКО на основе команды runcommand
        public VirtuosoCommand RunStart()
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            VirtuosoCommand runcommand = connection.CreateCommand();
            runcommand.CommandType = System.Data.CommandType.Text;
            var transaction = connection.BeginTransaction();
            runcommand.Transaction = transaction;
            return runcommand;
        }
        public void RunStop(VirtuosoCommand runcommand)
        {
            runcommand.Transaction.Commit();
            connection.Close();
        }
        // Используется для организации запросов внутри транзакции
        private IEnumerable<object[]> RunQuery(string sql, VirtuosoCommand runcommand)
        {
            runcommand.CommandText = sql;
            var reader = runcommand.ExecuteReader();

            int ncols = reader.FieldCount;
            object[] data = new object[ncols];
            while (reader.Read())
            {
                reader.GetValues(data);
                yield return data;
            }
            reader.Close();
        }
        public void Load(IEnumerable<Polar.RDFSimple.Triple> triples)
        {
            Execute("SPARQL CLEAR GRAPH <" + graph + ">");
            BufferForInsertCommand buffer = new BufferForInsertCommand(this, this.graph);
            buffer.InitBuffer();
            foreach (var triple in triples)
            {
                string two_parts = "<" + triple.subject + "> <" + triple.predicate + "> ";
                if (triple is Polar.RDFSimple.DTriple)
                {
                    buffer.AddCommandToBuffer(two_parts + "\"" + ((Polar.RDFSimple.DTriple)triple).data + "\" ");
                }
                else if (triple is Polar.RDFSimple.OTriple)
                {
                    buffer.AddCommandToBuffer(two_parts + "<" + ((Polar.RDFSimple.OTriple)triple).obj + "> ");
                }
                else throw new Exception("Assert error: 129231");
            }
            buffer.FlushBuffer();
        }
        
        public IEnumerable<object[]> GetReflections(int code, VirtuosoCommand runcommand)
        {
            string sparql = @"SPARQL SELECT ?doc WHERE {?reflection <reflected> <person" + code + "> . ?reflection <in_doc> ?doc . }";
            return RunQuery(sparql, runcommand);
        }
    }
}
