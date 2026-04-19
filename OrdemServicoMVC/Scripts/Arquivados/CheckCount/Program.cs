using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main()
    {
        var connStr = "Server=db41353.public.databaseasp.net; Database=db41353; User Id=db41353; Password=Ye7%h?E4J6!m; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
        
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM OrdensServico", conn);
        var count = (int)await cmd.ExecuteScalarAsync();
        
        Console.WriteLine($"\n--- RESULTADO SQL SERVER ---");
        Console.WriteLine($"Total de Ordens no SQL Server: {count}");
        Console.WriteLine($"----------------------------\n");
    }
}
