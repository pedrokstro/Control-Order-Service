import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# Fix priority chip in mobile header
content = content.replace(
    '@(ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Alta ? "<i class="bi bi-arrow-up-circle-fill" style="color: #dc3545;"></i> Alta" :\n                                  ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Media ? "<i class="bi bi-dash-circle-fill" style="color: #ffc107;"></i> Média" :\n                                  "<i class="bi bi-arrow-down-circle-fill" style="color: #0dcaf0;"></i> Baixa")',
    '@Html.Raw(ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Alta ? "<i class=\'bi bi-arrow-up-circle-fill\' style=\'color: #dc3545;\'></i> Alta" :\n                                  ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Media ? "<i class=\'bi bi-dash-circle-fill\' style=\'color: #ffc107;\'></i> Média" :\n                                  "<i class=\'bi bi-arrow-down-circle-fill\' style=\'color: #0dcaf0;\'></i> Baixa")'
)

# Fix priority chip in mobile body
content = content.replace(
    '@(ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Alta ? "<i class="bi bi-arrow-up-circle-fill" style="color: #dc3545;"></i> Alta" :\n                                              ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Media ? "<i class="bi bi-dash-circle-fill" style="color: #ffc107;"></i> Média" :\n                                              "<i class="bi bi-arrow-down-circle-fill" style="color: #0dcaf0;"></i> Baixa")',
    '@Html.Raw(ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Alta ? "<i class=\'bi bi-arrow-up-circle-fill\' style=\'color: #dc3545;\'></i> Alta" :\n                                              ordem.Prioridade == OrdemServicoMVC.Models.PrioridadeEnum.Media ? "<i class=\'bi bi-dash-circle-fill\' style=\'color: #ffc107;\'></i> Média" :\n                                              "<i class=\'bi bi-arrow-down-circle-fill\' style=\'color: #0dcaf0;\'></i> Baixa")'
)

# Fix status chip in mobile body
content = content.replace(
    '@(ordem.Status == OrdemServicoMVC.Models.StatusEnum.Aberta ? "<i class="bi bi-folder2-open"></i> Aberta" :\n                                              ordem.Status == OrdemServicoMVC.Models.StatusEnum.EmAndamento ? "<i class="bi bi-gear-fill"></i> Em andamento" :\n                                              "<i class="bi bi-check-circle-fill"></i> Concluída")',
    '@Html.Raw(ordem.Status == OrdemServicoMVC.Models.StatusEnum.Aberta ? "<i class=\'bi bi-folder2-open\'></i> Aberta" :\n                                              ordem.Status == OrdemServicoMVC.Models.StatusEnum.EmAndamento ? "<i class=\'bi bi-gear-fill\'></i> Em andamento" :\n                                              "<i class=\'bi bi-check-circle-fill\'></i> Concluída")'
)

# Fix prioridadeIcon in foreach
content = content.replace(
    'var prioridadeIcon = (int)prioridade == 3 ? "<i class=\'bi bi-arrow-up-circle-fill\' style=\'color: #dc3545;\'></i>" : (int)prioridade == 2 ? "<i class=\'bi bi-dash-circle-fill\' style=\'color: #ffc107;\'></i>" : "<i class=\'bi bi-arrow-down-circle-fill\' style=\'color: #0dcaf0;\'></i>";',
    'var prioridadeIcon = (int)prioridade == 3 ? "<i class=\\"bi bi-arrow-up-circle-fill\\" style=\\"color: #dc3545;\\"></i>" : (int)prioridade == 2 ? "<i class=\\"bi bi-dash-circle-fill\\" style=\\"color: #ffc107;\\"></i>" : "<i class=\\"bi bi-arrow-down-circle-fill\\" style=\\"color: #0dcaf0;\\"></i>";'
)

# Fix statusIcon in foreach
content = content.replace(
    'var statusIcon = status.Value == "1" ? "<i class=\'bi bi-folder2-open\'></i>" : status.Value == "2" ? "<i class=\'bi bi-gear-fill\'></i>" : "<i class=\'bi bi-check-circle-fill\'></i>";',
    'var statusIcon = status.Value == "1" ? "<i class=\\"bi bi-folder2-open\\"></i>" : status.Value == "2" ? "<i class=\\"bi bi-gear-fill\\"></i>" : "<i class=\\"bi bi-check-circle-fill\\"></i>";'
)

# Fix badges in desktop table
content = content.replace(
    '<span class="os-badge badge-status-aberta"><i class="bi bi-folder2-open"></i> Aberta</span>',
    '<span class="os-badge badge-status-aberta">@Html.Raw("<i class=\'bi bi-folder2-open\'></i> Aberta")</span>'
)
content = content.replace(
    '<span class="os-badge badge-status-andamento"><i class="bi bi-gear-fill"></i> Em Andamento</span>',
    '<span class="os-badge badge-status-andamento">@Html.Raw("<i class=\'bi bi-gear-fill\'></i> Em Andamento")</span>'
)
content = content.replace(
    '<span class="os-badge badge-status-concluida"><i class="bi bi-check-circle-fill"></i> Concluída</span>',
    '<span class="os-badge badge-status-concluida">@Html.Raw("<i class=\'bi bi-check-circle-fill\'></i> Concluída")</span>'
)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
