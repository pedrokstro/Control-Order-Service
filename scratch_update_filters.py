import re

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# Status Filter
status_pattern = r'<select class="form-select-modern" id="statusFilter" name="status" style="min-width: 140px;">'
status_replacement = '''<select class="ghost-select ghost-filter-status @(ViewBag.CurrentStatus?.ToString() == "1" ? "ghost-status-Aberta" : ViewBag.CurrentStatus?.ToString() == "2" ? "ghost-status-EmAndamento" : ViewBag.CurrentStatus?.ToString() == "3" ? "ghost-status-Concluida" : "ghost-select-neutral")" id="statusFilter" name="status" style="min-width: 140px;">'''
content = content.replace(status_pattern, status_replacement)

# Priority Filter
priority_pattern = r'<select class="form-select-modern" id="prioridadeFilter" name="prioridade" style="min-width: 160px;">'
priority_replacement = '''<select class="ghost-select ghost-filter-prioridade @(ViewBag.CurrentPrioridade?.ToString() == "1" ? "ghost-prioridade-Baixa" : ViewBag.CurrentPrioridade?.ToString() == "2" ? "ghost-prioridade-Media" : ViewBag.CurrentPrioridade?.ToString() == "3" ? "ghost-prioridade-Alta" : "ghost-select-neutral")" id="prioridadeFilter" name="prioridade" style="min-width: 160px;">'''
content = content.replace(priority_pattern, priority_replacement)

# Technician Filter
tech_pattern = r'<select class="form-select-modern" id="tecnicoFilter" name="tecnico" style="min-width: 140px;">'
tech_replacement = '''<select class="ghost-select ghost-tecnico @(string.IsNullOrEmpty(ViewBag.CurrentTecnico?.ToString()) ? "ghost-select-neutral" : "")" id="tecnicoFilter" name="tecnico" style="min-width: 140px;">'''
content = content.replace(tech_pattern, tech_replacement)

# Loja Filter
loja_pattern = r'<select class="form-select-modern" id="lojaFilter" name="loja" style="min-width: 120px;">'
loja_replacement = '''<select class="ghost-select ghost-select-simple @(string.IsNullOrEmpty(ViewBag.CurrentLoja?.ToString()) ? "ghost-select-neutral" : "")" id="lojaFilter" name="loja" style="min-width: 120px;">'''
content = content.replace(loja_pattern, loja_replacement)

# Setor Filter
setor_pattern = r'<select class="form-select-modern" id="setorFilter" name="setor" style="min-width: 140px;">'
setor_replacement = '''<select class="ghost-select ghost-select-simple @(string.IsNullOrEmpty(ViewBag.CurrentSetor?.ToString()) ? "ghost-select-neutral" : "")" id="setorFilter" name="setor" style="min-width: 140px;">'''
content = content.replace(setor_pattern, setor_replacement)

with open('OrdemServicoMVC/Views/OrdemServico/Index.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)
