# COS - Control Order Service 🚀

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Mobile First](https://img.shields.io/badge/UI-Mobile--First-brightgreen)

O **COS (Control Order Service)** é uma poderosa aplicação web desenvolvida em **ASP.NET Core MVC** projetada para facilitar a criação, delegação, monitoramento e gerenciamento de Ordens de Serviço (OS) com extrema facilidade, desempenho e segurança. 

Focado nos princípios modernos de *Mobile First* aliado ao estilo *Clean* (e anteriormente Brutalist, agora refinado como Premium/Glassmorphism), o COS traz a facilidade de um painel nativo de celular para o técnico operacional enquanto fornece relatórios completos para a gerência.

---

## 🌟 Funcionalidades Principais

* **📱 Progress Web App (PWA) e Mobile First:** Experiência nativa em dispositivos móveis, com *Bottom Sheets*, Bottom Navbars blur-effects e FABs amigáveis!
* **📸 Captura Nativa de Câmera:** Anexe fotos e vídeos na OS em tempo real diretamente da câmera do dispositivo de forma nativa e paralela à galeria de imagens.
* **🛡️ Sistema de Role-playing (RBAC):** Controle hierárquico preciso separando regras estritas entre `Admin` e `Funcionário`.
* **⚙️ Orquestração de Filas:** Gerenciamento de status, nível de criticidade e triagem de técnicos responsáveis.
* **💬 Painel de Mensagens e Follow-ups:** Mantenha um chat colaborativo contendo histórico e pendências vinculados especificamente para a resolução de cada OS.
* **📊 Dashboards e Gráficos:** Ferramentas que convertem ordens de serviço puras em conhecimento mensurável sobre performance de atendimento.

## 🛠️ Tecnologias e Stack

- **Backend:** C# e ASP.NET Core MVC 
- **Banco de Dados (ORM):** Entity Framework Core (Suporte configurável a SQL Server, MySQL e SQLite).
- **Frontend / Styling:** Bootstrap 5, Javascript Puro, CSS customizado e Mobile-Responsive Grids.
- **Integração Real-Time e Processamento:** Anexos validados no cliente e no servidor, APIs reativas e upload em chunks.

## 🚀 Como Executar Localmente

Siga o passo a passo para executar este projeto em sua máquina local.

**1. Clone o Repositório:**
```bash
git clone https://github.com/SeuUsuario/COS-ControlOrderService.git
cd COS-ControlOrderService
```

**2. Configure as Variáveis de Ambiente:**
Renomeie o modelo base de configurações para que sua string de conexão não seja injetada no repositório público:

* Duplique `OrdemServicoMVC/appsettings.Example.json`
* Renomeie para `appsettings.json`
* Preencha com sua string de conexão correta!

**3. Restaure as Dependências e Atualize o Banco (Migrations):**
```bash
cd OrdemServicoMVC
dotnet restore
dotnet ef database update
```

**4. Execute!**
```bash
dotnet run
```
Pronto! O terminal retornará o Localhost para acessar o painel. Abra em seu navegador e se divirta experimentando nossa UI fluída.

*(Nota: Como este projeto usa PWA, caso note que novos uploads de código não refletem no frontend localmente faça um* `Ctrl + F5` *para Hard Refresh ignorando cache do Service Worker!)*

## 📦 Changelog e Regras Globais
Sempre verificamos o [CHANGELOG.md](./OrdemServicoMVC/CHANGELOG.md) perante qualquer novo commit. Verifique-o para entender as correções mais recentes (ex: v2.3.25 implementou a câmera nativa).

---
**Desenvolvido com ❤️ sob princípios de clean Architecture e Mobile-First**
