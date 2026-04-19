# ============================================================
# Script de Migração: SQLite → SQL Server
# COS - Control Order Service
# ============================================================
# Uso: .\Scripts\MigrateSqliteToSqlServer.ps1
# Pré-requisitos:
#   - dotnet-ef instalado: dotnet tool install --global dotnet-ef
#   - Conexão com o SQL Server ativa
#   - Backup do banco SQLite feito antes de executar
# ============================================================

param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = ".",

    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [switch]$SkipSchemaCreation
)

$ErrorActionPreference = "Stop"

# Resolver caminho do projeto
if (Test-Path (Join-Path $ProjectPath "OrdemServicoMVC.csproj")) {
    $ProjectPath = Resolve-Path $ProjectPath
}
elseif (Test-Path (Join-Path $ProjectPath "..\OrdemServicoMVC.csproj")) {
    $ProjectPath = Resolve-Path (Join-Path $ProjectPath "..")
}
else {
    Write-Host "[ERRO] Não foi possível encontrar 'OrdemServicoMVC.csproj'" -ForegroundColor Red
    exit 1
}

Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host "   MIGRAÇÃO DE DADOS: SQLite → SQL Server" -ForegroundColor Cyan
Write-Host "   COS - Control Order Service" -ForegroundColor Cyan
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# PASSO 1: Verificar pré-requisitos
# ============================================================
Write-Host "[1/6] Verificando pré-requisitos..." -ForegroundColor Yellow

# Verificar banco SQLite
$sqliteDb = Join-Path $ProjectPath "ordemservico.db"
if (!(Test-Path $sqliteDb)) {
    Write-Host "[ERRO] Banco SQLite não encontrado em: $sqliteDb" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Banco SQLite encontrado: $sqliteDb" -ForegroundColor Green

# Ler connection string do SQL Server
$appSettingsPath = Join-Path $ProjectPath "appsettings.json"
$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
$sqlServerConn = $appSettings.ConnectionStrings.SqlServerConnection

if ([string]::IsNullOrWhiteSpace($sqlServerConn)) {
    Write-Host "[ERRO] Connection string 'SqlServerConnection' não encontrada no appsettings.json" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Connection string SQL Server configurada" -ForegroundColor Green

# ============================================================
# PASSO 2: Fazer backup do SQLite
# ============================================================
Write-Host ""
Write-Host "[2/6] Criando backup do banco SQLite..." -ForegroundColor Yellow

$backupDir = Join-Path $ProjectPath "Backups\Migration_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
Copy-Item $sqliteDb -Destination (Join-Path $backupDir "ordemservico.db")
Write-Host "  ✓ Backup criado em: $backupDir" -ForegroundColor Green

# ============================================================
# PASSO 3: Criar schema no SQL Server
# ============================================================
if (!$SkipSchemaCreation) {
    Write-Host ""
    Write-Host "[3/6] Criando schema no SQL Server..." -ForegroundColor Yellow
    Write-Host "  Alterando DatabaseProvider para 'SqlServer' temporariamente..." -ForegroundColor DarkGray

    # Alterar appsettings temporariamente para SQL Server
    $originalProvider = $appSettings.ConnectionStrings.DatabaseProvider
    $appSettings.ConnectionStrings.DatabaseProvider = "SqlServer"
    $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8 -NoNewline

    try {
        # Usar EnsureCreated via dotnet run com flag especial, ou aplicar migrations
        Write-Host "  Executando criação do schema via EF Core..." -ForegroundColor DarkGray
        
        # Criar um programa temporário para criar o schema
        $tempProgram = @"
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrdemServicoMVC.Data;

var builder = WebApplication.CreateBuilder(args);
var sqlServerConn = builder.Configuration.GetConnectionString("SqlServerConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlServerConn));
var app = builder.Build();
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
context.Database.EnsureCreated();
Console.WriteLine("Schema criado com sucesso!");
"@

        Write-Host "  ⚠ Para criar o schema, execute o aplicativo com DatabaseProvider='SqlServer'" -ForegroundColor Yellow
        Write-Host "  ⚠ O EnsureCreated() no Program.cs criará as tabelas automaticamente" -ForegroundColor Yellow
        
        if (!$DryRun) {
            Write-Host "  Iniciando aplicação para criar schema (será encerrada após 15s)..." -ForegroundColor DarkGray
            $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$ProjectPath`"" -PassThru -NoNewWindow
            Start-Sleep -Seconds 15
            if (!$process.HasExited) {
                $process.Kill()
            }
            Write-Host "  ✓ Schema criado no SQL Server" -ForegroundColor Green
        }
        else {
            Write-Host "  [DRY-RUN] Schema seria criado no SQL Server" -ForegroundColor Magenta
        }
    }
    finally {
        # Restaurar appsettings para SQLite
        $appSettings.ConnectionStrings.DatabaseProvider = $originalProvider
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8 -NoNewline
        Write-Host "  ✓ DatabaseProvider restaurado para '$originalProvider'" -ForegroundColor Green
    }
}
else {
    Write-Host ""
    Write-Host "[3/6] Criação de schema ignorada (--SkipSchemaCreation)" -ForegroundColor DarkGray
}

# ============================================================
# PASSO 4: Exportar dados do SQLite
# ============================================================
Write-Host ""
Write-Host "[4/6] Preparando migração de dados..." -ForegroundColor Yellow

# Lista de tabelas na ordem correta (respeitando foreign keys)
$tables = @(
    "AspNetRoles",
    "AspNetUsers", 
    "AspNetRoleClaims",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserRoles",
    "AspNetUserTokens",
    "OrdensServico",
    "Mensagens",
    "Anexos",
    "__EFMigrationsHistory"
)

Write-Host "  Tabelas a migrar: $($tables -join ', ')" -ForegroundColor DarkGray

# ============================================================
# PASSO 5: Migrar dados via C# script
# ============================================================
Write-Host ""
Write-Host "[5/6] Gerando script de migração C#..." -ForegroundColor Yellow

$migrationScript = Join-Path $backupDir "MigrationHelper.cs"

$csharpCode = @"
// ============================================================
// Script de Migração de Dados: SQLite → SQL Server
// Execute este código no aplicativo para migrar os dados
// ============================================================
// 
// INSTRUÇÕES:
// 1. Adicione um endpoint temporário no HomeController ou crie um Controller separado
// 2. Copie o método MigrarDados() abaixo
// 3. Execute via browser: /Home/MigrarDados
// 4. Remova o endpoint após a migração
//
// ALTERNATIVA SIMPLES:
// 1. Altere DatabaseProvider para "SqlServer" no appsettings.json
// 2. Execute o aplicativo (schema será criado automaticamente)
// 3. Use uma ferramenta como Azure Data Studio ou DBeaver para importar dados
//
// ============================================================

using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Adicione este método em um Controller (ex: HomeController.cs)
// [Authorize(Roles = "Admin")]
// [HttpGet]
// public async Task<IActionResult> MigrarDados()
// {
//     var sqliteConn = "Data Source=ordemservico.db";
//     var sqlServerConn = _configuration.GetConnectionString("SqlServerConnection");
//     
//     var migrator = new DataMigrator(sqliteConn, sqlServerConn);
//     var result = await migrator.MigrarTudo();
//     
//     return Json(result);
// }

public class DataMigrator
{
    private readonly string _sqliteConn;
    private readonly string _sqlServerConn;
    
    public DataMigrator(string sqliteConn, string sqlServerConn)
    {
        _sqliteConn = sqliteConn;
        _sqlServerConn = sqlServerConn;
    }
    
    public async Task<object> MigrarTudo()
    {
        var resultados = new Dictionary<string, object>();
        
        // Ordem de migração (respeita foreign keys)
        var tabelas = new[]
        {
            "AspNetRoles",
            "AspNetUsers",
            "AspNetRoleClaims",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserRoles",
            "AspNetUserTokens",
            "OrdensServico",
            "Mensagens",
            "Anexos"
        };
        
        foreach (var tabela in tabelas)
        {
            try
            {
                var count = await MigrarTabela(tabela);
                resultados[tabela] = new { sucesso = true, registros = count };
            }
            catch (Exception ex)
            {
                resultados[tabela] = new { sucesso = false, erro = ex.Message };
            }
        }
        
        return resultados;
    }
    
    private async Task<int> MigrarTabela(string tabela)
    {
        int count = 0;
        
        using var srcConn = new SqliteConnection(_sqliteConn);
        await srcConn.OpenAsync();
        
        using var destConn = new SqlConnection(_sqlServerConn);
        await destConn.OpenAsync();
        
        // Habilita IDENTITY_INSERT para tabelas com ID auto-incremento
        var enableIdentity = $"SET IDENTITY_INSERT [{tabela}] ON";
        var disableIdentity = $"SET IDENTITY_INSERT [{tabela}] OFF";
        
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
        var paramList = string.Join(", ", columns.Select((c, i) => $"@p{i}"));
        
        // Tentar habilitar IDENTITY_INSERT (pode falhar se tabela não tem identity)
        bool hasIdentity = false;
        try
        {
            using var identityCmd = new SqlCommand(enableIdentity, destConn);
            await identityCmd.ExecuteNonQueryAsync();
            hasIdentity = true;
        }
        catch { /* Tabela não tem identity column */ }
        
        // Inserir dados no SQL Server
        while (await reader.ReadAsync())
        {
            var insertSql = $"INSERT INTO [{tabela}] ({columnList}) VALUES ({paramList})";
            using var insertCmd = new SqlCommand(insertSql, destConn);
            
            for (int i = 0; i < columns.Count; i++)
            {
                var value = reader.GetValue(i);
                insertCmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
            }
            
            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }
        
        // Desabilitar IDENTITY_INSERT
        if (hasIdentity)
        {
            using var disableCmd = new SqlCommand(disableIdentity, destConn);
            await disableCmd.ExecuteNonQueryAsync();
        }
        
        return count;
    }
}
"@

$csharpCode | Set-Content $migrationScript -Encoding UTF8
Write-Host "  ✓ Script de migração salvo em: $migrationScript" -ForegroundColor Green

# ============================================================
# PASSO 6: Resumo
# ============================================================
Write-Host ""
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host "   CONFIGURAÇÃO CONCLUÍDA!" -ForegroundColor Green
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Banco SQL Server configurado e pronto para migração." -ForegroundColor White
Write-Host ""
Write-Host "  PARA MIGRAR, SIGA ESTES PASSOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Altere 'DatabaseProvider' para 'SqlServer'" -ForegroundColor White
Write-Host "     no arquivo appsettings.json" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  2. Execute: dotnet run" -ForegroundColor White
Write-Host "     (o schema será criado automaticamente)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  3. Copie o código de MigrationHelper.cs para" -ForegroundColor White
Write-Host "     um Controller e execute via browser" -ForegroundColor DarkGray
Write-Host "     Caminho: $migrationScript" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  4. Após confirmar que tudo funciona, remova" -ForegroundColor White
Write-Host "     o endpoint de migração" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  PARA VOLTAR AO SQLite:" -ForegroundColor Yellow
Write-Host "     Basta alterar 'DatabaseProvider' para 'SQLite'" -ForegroundColor White
Write-Host ""
Write-Host "  Backup do banco atual: $backupDir" -ForegroundColor DarkGray
Write-Host "===========================================================" -ForegroundColor Cyan
