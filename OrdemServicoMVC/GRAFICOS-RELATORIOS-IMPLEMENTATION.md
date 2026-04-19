# 📊 Gráficos de Relatórios - Plano de Implementação

## 🎯 Objetivo

Adicionar gráficos interativos ao sistema de relatórios para visualizar:
1. **Lojas mais ativas** - Ranking de lojas que mais abriram ordens por período
2. **Problemas mais frequentes** - Análise de descrições/títulos mais comuns
3. **Atividade por setor** - Distribuição de ordens por setor

## 📋 Estrutura de Dados

### ViewModels Criados

```csharp
// LojaAtividadeViewModel - Dados por loja
public class LojaAtividadeViewModel
{
    public string NomeLoja { get; set; }
    public int TotalOrdens { get; set; }
    public int OrdensAbertas { get; set; }
    public int OrdensEmAndamento { get; set; }
    public int OrdensConcluidas { get; set; }
    public double PercentualTotal { get; set; }
}

// ProblemaFrequenteViewModel - Problemas recorrentes
public class ProblemaFrequenteViewModel
{
    public string Descricao { get; set; }
    public int Quantidade { get; set; }
    public double PercentualTotal { get; set; }
    public List<string> LojasAfetadas { get; set; }
    public string SetorMaisAfetado { get; set; }
}

// SetorAtividadeViewModel - Dados por setor
public class SetorAtividadeViewModel
{
    public string NomeSetor { get; set; }
    public int TotalOrdens { get; set; }
    public double TempoMedioConclusao { get; set; }
    public double PercentualTotal { get; set; }
}

// GraficoDataViewModel - Formato Chart.js
public class GraficoDataViewModel
{
    public List<string> Labels { get; set; }
    public List<int> Data { get; set; }
    public List<string> BackgroundColors { get; set; }
    public List<string> BorderColors { get; set; }
}
```

## 🔧 Implementação Backend

### 1. Controller Actions (RelatorioController.cs)

```csharp
// GET: Relatório com gráficos
[HttpGet]
public async Task<IActionResult> Graficos(DateTime? dataInicio, DateTime? dataFim)
{
    // Define período padrão (últimos 30 dias)
    var inicio = dataInicio ?? DateTime.Now.AddDays(-30);
    var fim = dataFim ?? DateTime.Now;
    
    // Busca ordens do período
    var ordens = await _context.OrdensServico
        .Include(o => o.UsuarioCriador)
        .Include(o => o.TecnicoResponsavel)
        .Where(o => o.DataCriacao >= inicio && o.DataCriacao <= fim)
        .ToListAsync();
    
    // Calcula estatísticas
    var estatisticas = new RelatorioEstatisticasViewModel
    {
        TotalOrdens = ordens.Count,
        
        // Lojas mais ativas
        LojasAtividade = ordens
            .GroupBy(o => o.UsuarioCriador?.NomeLoja ?? "Sem loja")
            .Select(g => new LojaAtividadeViewModel
            {
                NomeLoja = g.Key,
                TotalOrdens = g.Count(),
                OrdensAbertas = g.Count(o => o.Status == StatusEnum.Aberta),
                OrdensEmAndamento = g.Count(o => o.Status == StatusEnum.EmAndamento),
                OrdensConcluidas = g.Count(o => o.Status == StatusEnum.Concluida),
                PercentualTotal = (double)g.Count() / ordens.Count * 100
            })
            .OrderByDescending(l => l.TotalOrdens)
            .Take(10)
            .ToList(),
        
        // Problemas mais frequentes (análise de palavras-chave)
        ProblemasFrequentes = AnalisarProblemasFrequentes(ordens),
        
        // Atividade por setor
        SetoresAtividade = ordens
            .GroupBy(o => o.Setor)
            .Select(g => new SetorAtividadeViewModel
            {
                NomeSetor = g.Key.ToString(),
                TotalOrdens = g.Count(),
                TempoMedioConclusao = g
                    .Where(o => o.DataConclusao.HasValue)
                    .Average(o => (o.DataConclusao.Value - o.DataCriacao).TotalDays),
                PercentualTotal = (double)g.Count() / ordens.Count * 100
            })
            .OrderByDescending(s => s.TotalOrdens)
            .ToList()
    };
    
    var viewModel = new RelatorioResultadoViewModel
    {
        Estatisticas = estatisticas,
        Filtros = new RelatorioFiltroViewModel
        {
            DataInicio = inicio,
            DataFim = fim
        }
    };
    
    return View(viewModel);
}

// Método auxiliar para análise de problemas frequentes
private List<ProblemaFrequenteViewModel> AnalisarProblemasFrequentes(List<OrdemServico> ordens)
{
    // Extrai palavras-chave das descrições
    var palavrasChave = new Dictionary<string, List<OrdemServico>>();
    
    foreach (var ordem in ordens)
    {
        var palavras = ExtrairPalavrasChave(ordem.Descricao);
        foreach (var palavra in palavras)
        {
            if (!palavrasChave.ContainsKey(palavra))
                palavrasChave[palavra] = new List<OrdemServico>();
            
            palavrasChave[palavra].Add(ordem);
        }
    }
    
    // Retorna top 10 problemas
    return palavrasChave
        .Select(kvp => new ProblemaFrequenteViewModel
        {
            Descricao = kvp.Key,
            Quantidade = kvp.Value.Count,
            PercentualTotal = (double)kvp.Value.Count / ordens.Count * 100,
            LojasAfetadas = kvp.Value
                .Select(o => o.UsuarioCriador?.NomeLoja ?? "Sem loja")
                .Distinct()
                .ToList(),
            SetorMaisAfetado = kvp.Value
                .GroupBy(o => o.Setor)
                .OrderByDescending(g => g.Count())
                .First()
                .Key
                .ToString()
        })
        .OrderByDescending(p => p.Quantidade)
        .Take(10)
        .ToList();
}

// Extrai palavras-chave relevantes
private List<string> ExtrairPalavrasChave(string texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        return new List<string>();
    
    // Remove stopwords e extrai termos relevantes
    var stopwords = new[] { "o", "a", "de", "da", "do", "em", "para", "com", "por" };
    
    return texto
        .ToLower()
        .Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(p => p.Length > 3 && !stopwords.Contains(p))
        .GroupBy(p => p)
        .OrderByDescending(g => g.Count())
        .Take(5)
        .Select(g => g.Key)
        .ToList();
}

// API endpoint para dados do gráfico (JSON)
[HttpGet]
public async Task<IActionResult> DadosGraficoLojas(DateTime? dataInicio, DateTime? dataFim)
{
    var inicio = dataInicio ?? DateTime.Now.AddDays(-30);
    var fim = dataFim ?? DateTime.Now;
    
    var ordens = await _context.OrdensServico
        .Include(o => o.UsuarioCriador)
        .Where(o => o.DataCriacao >= inicio && o.DataCriacao <= fim)
        .ToListAsync();
    
    var dados = ordens
        .GroupBy(o => o.UsuarioCriador?.NomeLoja ?? "Sem loja")
        .Select(g => new { Loja = g.Key, Total = g.Count() })
        .OrderByDescending(x => x.Total)
        .Take(10)
        .ToList();
    
    var grafico = new GraficoDataViewModel
    {
        Labels = dados.Select(d => d.Loja).ToList(),
        Data = dados.Select(d => d.Total).ToList(),
        BackgroundColors = GerarCoresSwissPunk(dados.Count),
        BorderColors = Enumerable.Repeat("#000000", dados.Count).ToList()
    };
    
    return Json(grafico);
}

// Gera paleta de cores Swiss Punk
private List<string> GerarCoresSwissPunk(int quantidade)
{
    var cores = new List<string>
    {
        "#FF0000", // Vermelho
        "#000000", // Preto
        "#FFD700", // Amarelo
        "#00FF00", // Verde
        "#FFFFFF", // Branco
        "#CC0000", // Vermelho escuro
        "#333333", // Cinza escuro
        "#E0E0E0", // Cinza claro
    };
    
    var resultado = new List<string>();
    for (int i = 0; i < quantidade; i++)
    {
        resultado.Add(cores[i % cores.Count]);
    }
    
    return resultado;
}
```

## 🎨 Implementação Frontend

### 1. View Graficos.cshtml

```html
@model OrdemServicoMVC.ViewModels.RelatorioResultadoViewModel
@{
    ViewData["Title"] = "Relatórios e Gráficos";
}

<!-- Filtros de Período -->
<div class="swiss-card mb-4">
    <h2 class="swiss-section-header">FILTROS DE PERÍODO</h2>
    
    <form method="get" class="swiss-grid">
        <div class="swiss-col-4">
            <label class="form-label">DATA INÍCIO</label>
            <input type="date" name="dataInicio" class="swiss-input" 
                   value="@Model.Filtros.DataInicio?.ToString("yyyy-MM-dd")">
        </div>
        
        <div class="swiss-col-4">
            <label class="form-label">DATA FIM</label>
            <input type="date" name="dataFim" class="swiss-input" 
                   value="@Model.Filtros.DataFim?.ToString("yyyy-MM-dd")">
        </div>
        
        <div class="swiss-col-4 d-flex align-items-end">
            <button type="submit" class="swiss-btn swiss-btn--red w-100">
                APLICAR FILTROS
            </button>
        </div>
    </form>
</div>

<!-- Grid de Gráficos -->
<div class="swiss-grid">
    <!-- Gráfico: Lojas Mais Ativas -->
    <div class="swiss-col-6">
        <div class="swiss-card">
            <h3 class="swiss-section-header">LOJAS MAIS ATIVAS</h3>
            <canvas id="graficoLojas" height="300"></canvas>
            
            <!-- Tabela de dados -->
            <div class="mt-3">
                <table class="swiss-table swiss-table--striped">
                    <thead>
                        <tr>
                            <th>LOJA</th>
                            <th>TOTAL</th>
                            <th>%</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var loja in Model.Estatisticas.LojasAtividade)
                        {
                            <tr>
                                <td>@loja.NomeLoja</td>
                                <td>@loja.TotalOrdens</td>
                                <td>@loja.PercentualTotal.ToString("F1")%</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    </div>
    
    <!-- Gráfico: Problemas Frequentes -->
    <div class="swiss-col-6">
        <div class="swiss-card">
            <h3 class="swiss-section-header">PROBLEMAS MAIS FREQUENTES</h3>
            <canvas id="graficoProblemas" height="300"></canvas>
            
            <!-- Lista de problemas -->
            <div class="mt-3">
                @foreach (var problema in Model.Estatisticas.ProblemasFrequentes)
                {
                    <div class="mb-2 p-2" style="border-left: 3px solid #FF0000;">
                        <strong>@problema.Descricao</strong>
                        <br>
                        <small>
                            @problema.Quantidade ocorrências 
                            (@problema.PercentualTotal.ToString("F1")%)
                            - Setor: @problema.SetorMaisAfetado
                        </small>
                    </div>
                }
            </div>
        </div>
    </div>
    
    <!-- Gráfico: Atividade por Setor -->
    <div class="swiss-col-12 mt-4">
        <div class="swiss-card">
            <h3 class="swiss-section-header">ATIVIDADE POR SETOR</h3>
            <canvas id="graficoSetores" height="200"></canvas>
        </div>
    </div>
</div>

@section Scripts {
    <!-- Chart.js -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
    
    <script>
        // Configuração Swiss Punk para Chart.js
        Chart.defaults.font.family = "'Helvetica Neue', 'Helvetica', 'Arial', sans-serif";
        Chart.defaults.font.weight = 'bold';
        Chart.defaults.color = '#000000';
        
        // Gráfico: Lojas Mais Ativas
        const ctxLojas = document.getElementById('graficoLojas').getContext('2d');
        new Chart(ctxLojas, {
            type: 'bar',
            data: {
                labels: @Html.Raw(Json.Serialize(Model.Estatisticas.LojasAtividade.Select(l => l.NomeLoja))),
                datasets: [{
                    label: 'Total de Ordens',
                    data: @Html.Raw(Json.Serialize(Model.Estatisticas.LojasAtividade.Select(l => l.TotalOrdens))),
                    backgroundColor: '#FF0000',
                    borderColor: '#000000',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { display: false },
                    title: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: '#E0E0E0' },
                        ticks: { font: { weight: 'bold' } }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { weight: 'bold' } }
                    }
                }
            }
        });
        
        // Gráfico: Problemas Frequentes
        const ctxProblemas = document.getElementById('graficoProblemas').getContext('2d');
        new Chart(ctxProblemas, {
            type: 'doughnut',
            data: {
                labels: @Html.Raw(Json.Serialize(Model.Estatisticas.ProblemasFrequentes.Select(p => p.Descricao))),
                datasets: [{
                    data: @Html.Raw(Json.Serialize(Model.Estatisticas.ProblemasFrequentes.Select(p => p.Quantidade))),
                    backgroundColor: ['#FF0000', '#000000', '#FFD700', '#00FF00', '#CC0000', '#333333'],
                    borderColor: '#000000',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: { font: { weight: 'bold' } }
                    }
                }
            }
        });
        
        // Gráfico: Setores
        const ctxSetores = document.getElementById('graficoSetores').getContext('2d');
        new Chart(ctxSetores, {
            type: 'horizontalBar',
            data: {
                labels: @Html.Raw(Json.Serialize(Model.Estatisticas.SetoresAtividade.Select(s => s.NomeSetor))),
                datasets: [{
                    label: 'Ordens por Setor',
                    data: @Html.Raw(Json.Serialize(Model.Estatisticas.SetoresAtividade.Select(s => s.TotalOrdens))),
                    backgroundColor: '#000000',
                    borderColor: '#FF0000',
                    borderWidth: 2
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { color: '#E0E0E0' }
                    },
                    y: {
                        grid: { display: false }
                    }
                }
            }
        });
    </script>
}
```

## 🎨 Estilo Swiss Punk para Gráficos

```css
/* swiss-punk-charts.css */

/* Container de gráficos */
.chart-container {
    position: relative;
    padding: var(--space-3);
    background: var(--swiss-white);
    border: var(--swiss-border-medium) solid var(--swiss-black);
}

/* Canvas responsivo */
canvas {
    max-width: 100%;
    height: auto !important;
}

/* Legendas customizadas */
.chart-legend {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-2);
    margin-top: var(--space-3);
}

.chart-legend-item {
    display: flex;
    align-items: center;
    gap: var(--space-1);
    font-family: var(--font-helvetica);
    font-weight: var(--weight-bold);
    font-size: var(--text-sm);
    text-transform: uppercase;
}

.chart-legend-color {
    width: 16px;
    height: 16px;
    border: 2px solid var(--swiss-black);
}

/* Tooltips customizados */
.chartjs-tooltip {
    background: var(--swiss-black) !important;
    color: var(--swiss-white) !important;
    border: 2px solid var(--swiss-red) !important;
    border-radius: 0 !important;
    font-family: var(--font-helvetica) !important;
    font-weight: var(--weight-bold) !important;
    padding: var(--space-1) var(--space-2) !important;
}
```

## 📊 Benefícios

### 1. **Insights Operacionais**
- Identifica lojas com maior demanda
- Detecta problemas recorrentes
- Otimiza alocação de recursos

### 2. **Tomada de Decisão**
- Dados visuais para gestores
- Tendências por período
- Comparação entre lojas/setores

### 3. **Melhoria Contínua**
- Identifica gargalos
- Prioriza treinamentos
- Reduz problemas frequentes

## 🚀 Próximos Passos

1. ✅ ViewModels criados
2. ⏳ Implementar Controller actions
3. ⏳ Criar view com gráficos Chart.js
4. ⏳ Aplicar estilo Swiss Punk
5. ⏳ Adicionar exportação de dados (PDF/Excel)
6. ⏳ Implementar cache para performance
7. ⏳ Testar com dados reais

## 💡 Melhorias Futuras

- **Gráficos de tendência temporal** (linha do tempo)
- **Comparação entre períodos** (mês atual vs anterior)
- **Alertas automáticos** (problemas críticos recorrentes)
- **Dashboard executivo** (KPIs principais)
- **Exportação de gráficos** (PNG/PDF)
- **Filtros avançados** (múltiplas lojas, setores)

---

**Desenvolvido seguindo princípios Swiss Punk**
- ✅ Visualização estruturada e clara
- ✅ Paleta limitada (vermelho/preto/amarelo)
- ✅ Tipografia bold e uppercase
- ✅ Grid de 12 colunas
- ✅ Dados organizados e funcionais
