using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Text;
using OptimizingSQLQuery.Classes;
using Microsoft.EntityFrameworkCore;


string sql = $"SELECT TOP 1000000 * FROM received WHERE status = 1 ORDER BY re_ref";


IEnumerable<IConfigurationSection> SqlNodes = Program.Configuration.GetSection("ConnectionStrings").GetSection("SqlNodes").GetChildren();


//Parallel processing is used to ensure the db is not overwhelmed with too many concurrent connections
ConcurrentBag<Received> results = new ConcurrentBag<Received>();

Parallel.ForEach(SqlNodes, Node =>
{
    Received[] result = DbQuery<Received>.Query(Node.Value, sql); //internal function to query db and return results
    foreach (var rec in result)
    {
        results.Add(rec);
    }
});

//batch queries reduce the number of individual times the db is accessed, improving performance
int batchSize = 1000;
List<Received> batch = new(batchSize);

foreach (Received rec in results)
{
    batch.Add(rec);
    if (batch.Count >= batchSize)
    {
        InsertBatch(batch);
        batch.Clear();
    }
}

//insert any remaining records
if (batch.Count > 0)
{
    InsertBatch(batch);
}

void InsertBatch(List<Received> batch)
{
    //Parameterized queries are used to prevent SQL injection and improve overall performance of the query
    string ConnectionString = null;
    using (SqlConnection connection = new SqlConnection(ConnectionString))
    {
        connection.Open();
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("INSERT INTO received_total (rt_msisdn, rt_message) VALUES ");

        for (int i = 0; i < batch.Count; i++)
        {
            if (i > 0)
                queryBuilder.Append(",");
            queryBuilder.AppendFormat("(@rt_msisdn{0}, @rt_message{0})", i);
        }

        using (SqlCommand command = new SqlCommand(queryBuilder.ToString(), connection))
        {
            for (int i = 0; i < batch.Count; i++)
            {
                command.Parameters.AddWithValue($"@rt_msisdn{i}", batch[i].re_fromnum);
                command.Parameters.AddWithValue($"@rt_message{i}", batch[i].re_message);
            }

            command.ExecuteNonQuery();
        }

        connection.Close();
    }
}
