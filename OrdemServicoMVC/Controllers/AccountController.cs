using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.ViewModels;
using System.Globalization;

namespace OrdemServicoMVC.Controllers
{
    // Controller responsável pela autenticação de usuários
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            IOptions<AppSettings> appSettings) : base(appSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // Se o usuário já está logado, redireciona para a página principal
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "OrdemServico");
            }

            // Limpa cookies de autenticação existentes
            await _signInManager.SignOutAsync();

            ViewData["ReturnUrl"] = returnUrl;
            
            // Cria o modelo de login com a lista de lojas
            var model = new LoginViewModel
            {
                Lojas = await GetLojasSelectList()
            };

            return View(model);
        }

        /// <summary>
        /// Processa o formulário de login do usuário
        /// Autentica baseado na loja selecionada e senha fornecida
        /// </summary>
        /// <param name="model">Dados do formulário de login</param>
        /// <param name="returnUrl">URL para redirecionamento após login bem-sucedido</param>
        /// <returns>Redirecionamento ou view de login com erros</returns>
        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                // Busca o usuário pelo nome da loja selecionada no dropdown
                var user = await _userManager.FindByNameAsync(model.LojaUsuario);
                
                if (user != null)
                {
                    // Tenta autenticar o usuário com a senha fornecida
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName!, 
                        model.Senha, 
                        model.LembrarMe, 
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Usuário {user.UserName} fez login com sucesso.");
                        
                        // Redireciona para a URL de retorno (se válida) ou página principal
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
                        return RedirectToAction("Index", "OrdemServico");
                    }
                }
                
                // Se chegou aqui, o login falhou (usuário não encontrado ou senha incorreta)
                ModelState.AddModelError(string.Empty, "Loja ou senha inválidos.");
            }

            // Se chegou aqui, algo falhou, reexibe o formulário com a lista de lojas
            model.Lojas = await GetLojasSelectList();
            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuário fez logout.");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new UserProfileViewModel
            {
                UserName = user.UserName ?? user.Email ?? "Usuário",
                Email = user.Email ?? string.Empty,
                NomeLoja = user.NomeLoja ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsAdmin = user.IsAdmin,
                IsTecnico = user.IsTecnico,
                MemberSince = user.DataCriacao,
                Roles = roles
            };

            return View(viewModel);
        }

        /// <summary>
        /// Método auxiliar para obter a lista de lojas disponíveis para login
        /// Filtra apenas usuários que não são técnicos e ordena de forma lógica
        /// </summary>
        /// <returns>Lista de SelectListItem com as lojas ordenadas</returns>
        private async Task<List<SelectListItem>> GetLojasSelectList()
        {
            // Obtém todos os usuários que não são técnicos (lojas e administrador)
            var usuariosLojas = _userManager.Users
                .Where(u => !u.IsTecnico)
                .ToList();
            
            // Cria a lista de seleção mapeando UserName para Value e NomeLoja para Text
            var lojas = usuariosLojas.Select(u => new SelectListItem
            {
                Value = u.UserName,    // Username usado para autenticação
                Text = FormatLojaNome(u.NomeLoja)
            }).OrderBy(x =>
            {
                // 1. Administrador sempre primeiro
                if (x.Value?.Equals("Administrador", StringComparison.OrdinalIgnoreCase) == true ||
                    x.Text.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return "0";
                }

                // 2. Lojas numeradas em ordem crescente (Loja 1 · Matriz)
                if (x.Text.StartsWith("Loja ", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = x.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 2 && int.TryParse(tokens[1], out int lojaNum))
                    {
                        return $"1{lojaNum:D3}";
                    }
                }

                // 3. Outros casos ordenados alfabeticamente por último
                return "2" + x.Text;
            }).ToList();

            // Adiciona uma opção padrão no início da lista
            lojas.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Selecione uma loja..."
            });

            return lojas;
        }

        private static string FormatLojaNome(string? nomeLoja)
        {
            if (string.IsNullOrWhiteSpace(nomeLoja))
            {
                return "Loja";
            }

            if (nomeLoja.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return "Administrador";
            }

            var textInfo = new CultureInfo("pt-BR").TextInfo;
            var parts = nomeLoja.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2 && parts[0].StartsWith("LOJA", StringComparison.OrdinalIgnoreCase))
            {
                var numeroTokens = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var numero = numeroTokens.Length >= 2 ? numeroTokens[1] : string.Empty;
                var lojaLabel = string.IsNullOrEmpty(numero)
                    ? textInfo.ToTitleCase(parts[0].ToLower())
                    : $"Loja {numero}";

                var descricao = textInfo.ToTitleCase(parts[1].ToLower());
                return $"{lojaLabel} · {descricao}";
            }

            return textInfo.ToTitleCase(nomeLoja.ToLower());
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}