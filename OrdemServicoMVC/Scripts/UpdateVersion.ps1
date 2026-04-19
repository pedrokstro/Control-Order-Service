# Script PowerShell para atualizar a versao do projeto COS - Control Order Service
# Uso: .\Scripts\UpdateVersion.ps1 -NewVersion "1.2.0"
# Ou se estiver na pasta Scripts: .\UpdateVersion.ps1 -NewVersion "1.2.0"

param(
    [Parameter(Mandatory = $true)]
    [string]$NewVersion,

    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = ".", 

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# 1. Validação do formato da versão
if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "[ERRO] Formato de versão inválido. Use X.Y.Z (ex: 2.3.2)" -ForegroundColor Red
    exit 1
}

# Resolver caminho absoluto do projeto
# Cenário 1: Executando da raiz do projeto (onde está o .csproj)
if (Test-Path (Join-Path $ProjectPath "OrdemServicoMVC.csproj")) {
    $ProjectPath = Resolve-Path $ProjectPath
}
# Cenário 2: Executando da pasta Scripts (o projeto está um nível acima)
elseif (Test-Path (Join-Path $ProjectPath "..\OrdemServicoMVC.csproj")) {
    $ProjectPath = Resolve-Path (Join-Path $ProjectPath "..")
}
else {
    Write-Host "[ERRO] Não foi possível encontrar 'OrdemServicoMVC.csproj' em '$ProjectPath' ou '../'. Por favor, execute da raiz do projeto ou especifique -ProjectPath." -ForegroundColor Red
    exit 1
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "   ATUALIZADOR DE VERSÃO - COS ORDER SERVICE" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Nova Versão: $NewVersion" -ForegroundColor Yellow
Write-Host "Diretório:   $ProjectPath" -ForegroundColor Gray
Write-Host ""

# 2. Definição dos Arquivos e Padrões Regex
$targets = @(
    @{
        Name        = "appsettings.json"
        Path        = Join-Path $ProjectPath "appsettings.json"
        Regex       = '"Version":\s*"\d+\.\d+\.\d+"'
        Replacement = '"Version": "{0}"'
    },
    @{
        Name        = "_Layout.cshtml"
        Path        = Join-Path $ProjectPath "Views\Shared\_Layout.cshtml"
        Regex       = 'v@\(ViewBag\.AppVersion \?\? "\d+\.\d+\.\d+"\)'
        Replacement = 'v@(ViewBag.AppVersion ?? "{0}")'
    },
    @{
        Name        = "OrdemServicoMVC.csproj"
        Path        = Join-Path $ProjectPath "OrdemServicoMVC.csproj"
        Regex       = '<Version>\d+\.\d+\.\d+</Version>'
        Replacement = '<Version>{0}</Version>'
    }
)

# 3. Verificação de existência dos arquivos
foreach ($target in $targets) {
    if (!(Test-Path $target.Path)) {
        Write-Host "[ERRO] Arquivo crítico não encontrado: $($target.Path)" -ForegroundColor Red
        exit 1
    }
}

# 4. Confirmação
if (!$Force) {
    $confirm = Read-Host "Deseja aplicar a versão $NewVersion nestes arquivos? (S/N)"
    if ($confirm -notmatch '^[sS]') {
        Write-Host "Cancelado pelo usuário." -ForegroundColor Yellow
        exit 0
    }
}

# 5. Backup e Atualização
$backupDir = Join-Path $ProjectPath "Backups\VersionUpdate_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
Write-Host "Backup criado em: $backupDir" -ForegroundColor DarkGray

foreach ($target in $targets) {
    $file = $target.Path
    $fileName = $target.Name
    
    # Backup
    Copy-Item $file -Destination (Join-Path $backupDir $fileName)
    
    # Leitura
    $content = Get-Content $file -Raw -Encoding UTF8
    
    # Substituição
    $newString = $target.Replacement -f $NewVersion
    if ($content -match $target.Regex) {
        $newContent = $content -replace $target.Regex, $newString
        
        # Escrita
        Set-Content -Path $file -Value $newContent -Encoding UTF8 -NoNewline
        Write-Host "[OK] Atualizado $fileName" -ForegroundColor Green
    }
    else {
        Write-Host "[AVISO] Padrão de versão não encontrado em $fileName" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Atualização concluída com sucesso! Versão: $NewVersion" -ForegroundColor Green
Write-Host "Não esqueça de compilar o projeto: dotnet build" -ForegroundColor Cyan