using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MigrationTool
{
    class Program
    {
        // Connection strings
        static readonly string SqliteConn = @"Data Source=..\..\ordemservico_prod.db";
        static readonly string SqlServerConn = "Server=db41353.public.databaseasp.net; Database=db41353; User Id=db41353; Password=Ye7%h?E4J6!m; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

        // Tabelas na ordem correta (respeitando foreign keys)
        static readonly string[] Tabelas = new[]
        {
            "AspNetRoles",
            "AspNetUsers",
            "AspNetUserRoles",
            "AspNetRoleClaims",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserTokens",
            "OrdensServico",
            "Mensagens",
            "Anexos"
        };

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================");
            Console.WriteLine("  MIGRAÇÃO SQLite → SQL Server");
            Console.WriteLine("  COS - Control Order Service");
            Console.WriteLine("============================================");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Testar conexão SQLite
            Console.Write("[1] Testando conexão SQLite... ");
            try
            {
                using var sqliteConn = new SqliteConnection(SqliteConn);
                await sqliteConn.OpenAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERRO: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // 2. Testar conexão SQL Server
            Console.Write("[2] Testando conexão SQL Server... ");
            try
            {
                using var sqlConn = new SqlConnection(SqlServerConn);
                await sqlConn.OpenAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"OK! ({sqlConn.ServerVersion})");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERRO: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // 3. Contar registros de origem
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[3] Registros no SQLite (origem):");
            Console.ResetColor();
            
            using (var srcConn = new SqliteConnection(SqliteConn))
            {
                await srcConn.OpenAsync();
                foreach (var tabela in Tabelas)
                {
                    using var cmd = srcConn.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM [{tabela}]";
                    try
                    {
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        Console.WriteLine($"  {tabela}: {count} registros");
                    }
                    catch
                    {
                        Console.WriteLine($"  {tabela}: (tabela não existe)");
                    }
                }
            }

            // 4. Migrar dados
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[4] Migrando dados...");
            Console.ResetColor();

            var resultados = new Dictionary<string, (bool sucesso, int count, string? erro)>();

            foreach (var tabela in Tabelas)
            {
                try
                {
                    var count = await MigrarTabela(tabela);
                    resultados[tabela] = (true, count, null);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {tabela}: {count} registros migrados");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    resultados[tabela] = (false, 0, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {tabela}: ERRO - {ex.Message}");
                    Console.ResetColor();
                }
            }

            // 5. Verificar dados no destino
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[5] Verificando dados no SQL Server (destino):");
            Console.ResetColor();

            using (var destConn = new SqlConnection(SqlServerConn))
            {
                await destConn.OpenAsync();
                foreach (var tabela in Tabelas)
                {
                    using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [{tabela}]", destConn);
                    try
                    {
                        var count = (int)await cmd.ExecuteScalarAsync()!;
                        Console.WriteLine($"  {tabela}: {count} registros");
                    }
                    catch
                    {
                        Console.WriteLine($"  {tabela}: (erro ao consultar)");
                    }
                }
            }

            // 6. Resumo
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  MIGRAÇÃO CONCLUÍDA!");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================");
            Console.ResetColor();

            var totalMigrados = resultados.Where(r => r.Value.sucesso).Sum(r => r.Value.count);
            var totalErros = resultados.Count(r => !r.Value.sucesso);
            Console.WriteLine($"  Total migrado: {totalMigrados} registros");
            if (totalErros > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Erros: {totalErros} tabela(s)");
                Console.ResetColor();
            }
        }

        static async Task<int> MigrarTabela(string tabela)
        {
            int count = 0;

            using var srcConn = new SqliteConnection(SqliteConn);
            await srcConn.OpenAsync();

            using var destConn = new SqlConnection(SqlServerConn);
            await destConn.OpenAsync();

            // Verificar se a tabela tem dados no destino e limpar
            using (var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM [{tabela}]", destConn))
            {
                var existing = (int)await checkCmd.ExecuteScalarAsync()!;
                if (existing > 0)
                {
                    using var deleteCmd = new SqlCommand($"DELETE FROM [{tabela}]", destConn);
                    await deleteCmd.ExecuteNonQueryAsync();
                }
            }

            // Ler dados do SQLite
            using var readCmd = srcConn.CreateCommand();
            readCmd.CommandText = $"SELECT * FROM [{tabela}]";
            using var reader = await readCmd.ExecuteReaderAsync();

            if (!reader.HasRows) return 0;

            // Obter nomes das colunas
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
            var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

            // Tentar habilitar IDENTITY_INSERT
            bool hasIdentity = false;
            try
            {
                using var identityCmd = new SqlCommand($"SET IDENTITY_INSERT [{tabela}] ON", destConn);
                await identityCmd.ExecuteNonQueryAsync();
                hasIdentity = true;
            }
            catch
            {
                // Tabela não tem identity column
            }

            // Inserir dados
            while (await reader.ReadAsync())
            {
                var insertSql = hasIdentity
                    ? $"SET IDENTITY_INSERT [{tabela}] ON; INSERT INTO [{tabela}] ({columnList}) VALUES ({paramList}); SET IDENTITY_INSERT [{tabela}] OFF;"
                    : $"INSERT INTO [{tabela}] ({columnList}) VALUES ({paramList})";

                using var insertCmd = new SqlCommand(insertSql, destConn);

                for (int i = 0; i < columns.Count; i++)
                {
                    var value = reader.GetValue(i);

                    if (value is DBNull)
                    {
                        insertCmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                    }
                    else if (value is byte[] byteArr)
                    {
                        var param = insertCmd.Parameters.Add($"@p{i}", System.Data.SqlDbType.VarBinary, -1);
                        param.Value = byteArr;
                    }
                    else
                    {
                        insertCmd.Parameters.AddWithValue($"@p{i}", value);
                    }
                }

                await insertCmd.ExecuteNonQueryAsync();
                count++;
            }

            // Desabilitar IDENTITY_INSERT
            if (hasIdentity)
            {
                try
                {
                    using var disableCmd = new SqlCommand($"SET IDENTITY_INSERT [{tabela}] OFF", destConn);
                    await disableCmd.ExecuteNonQueryAsync();
                }
                catch { }
            }

            return count;
        }
    }
}
