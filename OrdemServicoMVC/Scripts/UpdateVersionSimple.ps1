# Script para atualizar versão do projeto COS automaticamente
# Incrementa automaticamente a versão patch (ex: 1.0.0 -> 1.0.1)

param(
    [string]$AutoConfirm = ""
)

Write-Host "=== ATUALIZADOR DE VERSÃO COS ===" -ForegroundColor Green
Write-Host ""

# Caminho para o arquivo appsettings.json
$appsettingsPath = ".\appsettings.json"

# Verifica se o arquivo existe
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "Erro: Arquivo appsettings.json não encontrado!" -ForegroundColor Red
    Write-Host "Certifique-se de executar o script no diretório raiz do projeto." -ForegroundColor Yellow
    exit 1
}

# Lê o conteúdo do arquivo appsettings.json
$jsonContent = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

# Extrai a versão atual
$currentVersion = $jsonContent.AppSettings.Version
Write-Host "Versão atual: $currentVersion" -ForegroundColor Cyan

# Divide a versão em partes (major.minor.patch)
$versionParts = $currentVersion.Split('.')
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1] 
$patch = [int]$versionParts[2]

# Incrementa a versão patch
$patch++
$newVersion = "$major.$minor.$patch"

Write-Host "Nova versão: $newVersion" -ForegroundColor Green
Write-Host ""

# Solicita confirmação do usuário (ou usa parâmetro)
if ($AutoConfirm -ne "") {
    $confirmation = $AutoConfirm
    Write-Host "Deseja continuar com a atualização? (s/N): $confirmation"
} else {
    $confirmation = Read-Host "Deseja continuar com a atualização? (s/N)"
}

# Processa a resposta - se confirmou, atualiza
if ($confirmation -eq "s" -or $confirmation -eq "S") {
    Write-Host ""
    Write-Host "Atualizando versão..." -ForegroundColor Yellow
    
    # Atualiza a versão no objeto JSON
    $jsonContent.AppSettings.Version = $newVersion
    
    # Converte de volta para JSON e salva
    $jsonContent | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8
    
    # Atualiza também o arquivo _Layout.cshtml
    $layoutPath = ".\Views\Shared\_Layout.cshtml"
    if (Test-Path $layoutPath) {
        $layoutContent = Get-Content $layoutPath -Raw
        $layoutContent = $layoutContent -replace "v$currentVersion", "v$newVersion"
        Set-Content $layoutPath $layoutContent -Encoding UTF8
        Write-Host "Arquivo _Layout.cshtml atualizado" -ForegroundColor Green
    }
    
    Write-Host "Versão atualizada com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos passos recomendados:" -ForegroundColor Cyan
    Write-Host "  1. Teste: dotnet run" -ForegroundColor White
    Write-Host "  2. Commit das alterações" -ForegroundColor White
} else {
    # Se não confirmou, cancela
    Write-Host ""
    Write-Host "Operação cancelada pelo usuário." -ForegroundColor Red
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Cyan
    Write-Host "  1. Teste: dotnet run" -ForegroundColor White
    Write-Host "  2. Commit das alterações" -ForegroundColor White
}