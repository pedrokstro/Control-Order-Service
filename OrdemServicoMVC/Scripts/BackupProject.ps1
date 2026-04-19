# Script PowerShell para Backup Completo do Projeto COS - Control Order Service
# Uso: .\BackupProject.ps1 [-BackupPath "C:\Backups"] [-IncludeDatabase] [-Compress]

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = "C:\Backups\COS-Backup",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$Compress,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

# Configuracoes globais
$ProjectRoot = "F:\CURSOR\COS-Control Order Service"
$BackupTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFolderName = "COS-Backup_$BackupTimestamp"
$FullBackupPath = Join-Path $BackupPath $BackupFolderName

# Pastas e arquivos a serem EXCLUIDOS do backup
$ExcludedFolders = @(
    "bin",           # Arquivos compilados
    "obj",           # Arquivos de build temporarios
    ".vs",           # Cache do Visual Studio
    "logs",          # Arquivos de log
    "backup_*",      # Backups anteriores
    "publish",       # Arquivos de publicacao
    "node_modules",  # Dependencias Node.js (se houver)
    "tmp",           # Arquivos temporarios
    ".git"           # Repositorio Git (se houver)
)

$ExcludedFiles = @(
    "*.log",         # Arquivos de log
    "*.tmp",         # Arquivos temporarios
    "*.cache",       # Arquivos de cache
    "*.user",        # Configuracoes de usuario do VS
    "*.suo",         # Solution User Options
    "*.db-shm",      # SQLite shared memory
    "*.db-wal"       # SQLite write-ahead log
)

# Funcao para exibir mensagens coloridas
function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White",
        [string]$Prefix = "[INFO]"
    )
    Write-Host "$Prefix $Message" -ForegroundColor $Color
}

# Funcao para verificar se um item deve ser excluido
function Should-Exclude {
    param(
        [string]$ItemPath,
        [string]$ItemName,
        [bool]$IsDirectory
    )
    
    if ($IsDirectory) {
        # Verifica pastas excluidas
        foreach ($excludedFolder in $ExcludedFolders) {
            if ($ItemName -like $excludedFolder) {
                return $true
            }
        }
    } else {
        # Verifica arquivos excluidos
        foreach ($excludedFile in $ExcludedFiles) {
            if ($ItemName -like $excludedFile) {
                return $true
            }
        }
    }
    
    return $false
}

# Funcao para copiar arquivos com exclusoes
function Copy-ProjectFiles {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )
    
    $copiedFiles = 0
    $skippedItems = 0
    
    Write-ColorMessage "Copiando arquivos do projeto..." "Yellow" "[BACKUP]"
    
    # Obtem todos os itens recursivamente
    $allItems = Get-ChildItem -Path $SourcePath -Recurse -Force
    $totalItems = $allItems.Count
    $currentItem = 0
    
    foreach ($item in $allItems) {
        $currentItem++
        $relativePath = $item.FullName.Substring($SourcePath.Length + 1)
        $destinationItemPath = Join-Path $DestinationPath $relativePath
        
        # Progresso
        if ($currentItem % 100 -eq 0) {
            $progress = [math]::Round(($currentItem / $totalItems) * 100, 1)
            Write-Progress -Activity "Copiando arquivos" -Status "$progress% - $relativePath" -PercentComplete $progress
        }
        
        # Verifica se deve excluir
        if (Should-Exclude -ItemPath $item.FullName -ItemName $item.Name -IsDirectory $item.PSIsContainer) {
            $skippedItems++
            continue
        }
        
        try {
            if ($item.PSIsContainer) {
                # Cria diretorio se nao existir
                if (!(Test-Path $destinationItemPath)) {
                    New-Item -ItemType Directory -Path $destinationItemPath -Force | Out-Null
                }
            } else {
                # Cria diretorio pai se necessario
                $parentDir = Split-Path $destinationItemPath -Parent
                if (!(Test-Path $parentDir)) {
                    New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
                }
                
                # Copia arquivo
                Copy-Item -Path $item.FullName -Destination $destinationItemPath -Force
                $copiedFiles++
            }
        }
        catch {
            Write-ColorMessage "Erro ao copiar: $relativePath - $($_.Exception.Message)" "Red" "[ERRO]"
        }
    }
    
    Write-Progress -Activity "Copiando arquivos" -Completed
    
    return @{
        CopiedFiles = $copiedFiles
        SkippedItems = $skippedItems
    }
}

# Funcao para fazer backup do banco de dados
function Backup-Database {
    param([string]$BackupFolder)
    
    $dbPath = Join-Path $ProjectRoot "OrdemServicoMVC\ordemservico.db"
    
    if (Test-Path $dbPath) {
        Write-ColorMessage "Fazendo backup do banco de dados..." "Yellow" "[DATABASE]"
        
        $dbBackupPath = Join-Path $BackupFolder "Database"
        New-Item -ItemType Directory -Path $dbBackupPath -Force | Out-Null
        
        # Copia o arquivo principal do banco
        Copy-Item -Path $dbPath -Destination (Join-Path $dbBackupPath "ordemservico.db") -Force
        
        # Cria dump SQL como backup adicional
        $sqlDumpPath = Join-Path $dbBackupPath "ordemservico_dump.sql"
        
        try {
            # Tenta usar sqlite3 se disponivel
            $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
            if ($sqliteCmd) {
                & sqlite3 $dbPath ".dump" | Out-File -FilePath $sqlDumpPath -Encoding UTF8
                Write-ColorMessage "Dump SQL criado com sucesso" "Green" "[DATABASE]"
            } else {
                Write-ColorMessage "sqlite3 nao encontrado - apenas arquivo .db copiado" "Yellow" "[DATABASE]"
            }
        }
        catch {
            Write-ColorMessage "Erro ao criar dump SQL: $($_.Exception.Message)" "Red" "[DATABASE]"
        }
        
        return $true
    } else {
        Write-ColorMessage "Banco de dados nao encontrado em: $dbPath" "Yellow" "[DATABASE]"
        return $false
    }
}

# Funcao para criar arquivo ZIP
function Create-ZipBackup {
    param(
        [string]$SourceFolder,
        [string]$ZipPath
    )
    
    Write-ColorMessage "Compactando backup..." "Yellow" "[ZIP]"
    
    try {
        # Remove arquivo ZIP existente se houver
        if (Test-Path $ZipPath) {
            Remove-Item $ZipPath -Force
        }
        
        # Cria arquivo ZIP
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($SourceFolder, $ZipPath)
        
        # Obtem tamanho do arquivo ZIP
        $zipSize = (Get-Item $ZipPath).Length
        $zipSizeMB = [math]::Round($zipSize / 1MB, 2)
        
        Write-ColorMessage "Arquivo ZIP criado: $ZipPath ($zipSizeMB MB)" "Green" "[ZIP]"
        
        # Remove pasta temporaria
        Remove-Item $SourceFolder -Recurse -Force
        Write-ColorMessage "Pasta temporaria removida" "Green" "[ZIP]"
        
        return $true
    }
    catch {
        Write-ColorMessage "Erro ao criar ZIP: $($_.Exception.Message)" "Red" "[ZIP]"
        return $false
    }
}

# Funcao para gerar relatorio do backup
function Generate-BackupReport {
    param(
        [hashtable]$BackupStats,
        [string]$BackupFolder,
        [bool]$DatabaseBackedUp,
        [bool]$Compressed
    )
    
    $reportPath = Join-Path $BackupFolder "backup-report.txt"
    
    $report = @"
========================================
RELATORIO DE BACKUP - COS PROJECT
========================================

Data/Hora: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")
Projeto: COS - Control Order Service
Origem: $ProjectRoot
Destino: $BackupFolder

ESTATISTICAS:
- Arquivos copiados: $($BackupStats.CopiedFiles)
- Itens ignorados: $($BackupStats.SkippedItems)
- Banco de dados: $(if($DatabaseBackedUp) { "Incluido" } else { "Nao incluido" })
- Compactacao: $(if($Compressed) { "Ativada" } else { "Desativada" })

PASTAS EXCLUIDAS:
$($ExcludedFolders -join ", ")

TIPOS DE ARQUIVO EXCLUIDOS:
$($ExcludedFiles -join ", ")

OBSERVACOES:
- Backup criado com sucesso
- Verifique a integridade dos arquivos importantes
- Mantenha este backup em local seguro

========================================
"@

    try {
        $report | Out-File -FilePath $reportPath -Encoding UTF8
        Write-ColorMessage "Relatorio salvo em: $reportPath" "Green" "[RELATORIO]"
    }
    catch {
        Write-ColorMessage "Erro ao salvar relatorio: $($_.Exception.Message)" "Red" "[RELATORIO]"
    }
}

# FUNCAO PRINCIPAL
function Start-ProjectBackup {
    Write-ColorMessage "========================================" "Cyan"
    Write-ColorMessage "BACKUP COMPLETO DO PROJETO COS" "Cyan"
    Write-ColorMessage "========================================" "Cyan"
    Write-ColorMessage "Origem: $ProjectRoot" "White"
    Write-ColorMessage "Destino: $FullBackupPath" "White"
    Write-ColorMessage "Incluir DB: $(if($IncludeDatabase) { 'Sim' } else { 'Nao' })" "White"
    Write-ColorMessage "Compactar: $(if($Compress) { 'Sim' } else { 'Nao' })" "White"
    Write-ColorMessage ""
    
    # Verifica se o projeto existe
    if (!(Test-Path $ProjectRoot)) {
        Write-ColorMessage "Projeto nao encontrado em: $ProjectRoot" "Red" "[ERRO]"
        exit 1
    }
    
    # Solicita confirmacao (se nao for forcado)
    if (-not $Force) {
        $confirmation = Read-Host "Deseja continuar com o backup? (s/N)"
        if ($confirmation -notmatch '^[sS]$') {
            Write-ColorMessage "Backup cancelado pelo usuario" "Red" "[CANCELADO]"
            exit 0
        }
    }
    
    # Cria diretorio de backup
    try {
        New-Item -ItemType Directory -Path $FullBackupPath -Force | Out-Null
        Write-ColorMessage "Diretorio de backup criado: $FullBackupPath" "Green" "[SETUP]"
    }
    catch {
        Write-ColorMessage "Erro ao criar diretorio de backup: $($_.Exception.Message)" "Red" "[ERRO]"
        exit 1
    }
    
    # Inicia o backup
    $startTime = Get-Date
    
    # Copia arquivos do projeto
    $backupStats = Copy-ProjectFiles -SourcePath $ProjectRoot -DestinationPath $FullBackupPath
    
    # Backup do banco de dados (se solicitado)
    $databaseBackedUp = $false
    if ($IncludeDatabase) {
        $databaseBackedUp = Backup-Database -BackupFolder $FullBackupPath
    }
    
    # Gera relatorio
    Generate-BackupReport -BackupStats $backupStats -BackupFolder $FullBackupPath -DatabaseBackedUp $databaseBackedUp -Compressed $Compress
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    # Compacta se solicitado
    if ($Compress) {
        $zipPath = "$FullBackupPath.zip"
        $compressed = Create-ZipBackup -SourceFolder $FullBackupPath -ZipPath $zipPath
        
        if ($compressed) {
            $finalPath = $zipPath
        } else {
            $finalPath = $FullBackupPath
        }
    } else {
        $finalPath = $FullBackupPath
    }
    
    # Relatorio final
    Write-ColorMessage ""
    Write-ColorMessage "========================================" "Green"
    Write-ColorMessage "BACKUP CONCLUIDO COM SUCESSO!" "Green"
    Write-ColorMessage "========================================" "Green"
    Write-ColorMessage "Tempo decorrido: $($duration.ToString('hh\:mm\:ss'))" "Green"
    Write-ColorMessage "Arquivos copiados: $($backupStats.CopiedFiles)" "Green"
    Write-ColorMessage "Itens ignorados: $($backupStats.SkippedItems)" "Green"
    Write-ColorMessage "Local do backup: $finalPath" "Green"
    Write-ColorMessage ""
    Write-ColorMessage "PROXIMOS PASSOS:" "Yellow"
    Write-ColorMessage "1. Verifique a integridade do backup" "White"
    Write-ColorMessage "2. Armazene em local seguro" "White"
    Write-ColorMessage "3. Teste a restauracao periodicamente" "White"
}

# Executa o backup
try {
    Start-ProjectBackup
}
catch {
    Write-ColorMessage "Erro critico durante o backup: $($_.Exception.Message)" "Red" "[ERRO]"
    exit 1
}