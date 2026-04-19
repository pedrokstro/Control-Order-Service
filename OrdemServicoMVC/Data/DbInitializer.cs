using Microsoft.AspNetCore.Identity;
using OrdemServicoMVC.Models;

namespace OrdemServicoMVC.Data
{
    // Classe responsável por inicializar o banco de dados com dados padrão
    public static class DbInitializer
    {
        // Método para popular o banco com usuários e roles padrão
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            // Obtém os serviços necessários
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            // Cria as roles se não existirem
            await CreateRoles(roleManager);
            
            // Cria os usuários padrão se não existirem
            await CreateUsers(userManager);
        }
        
        // Método para criar as roles Admin e User
        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "User" };
            
            foreach (var roleName in roleNames)
            {
                // Verifica se a role já existe
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Cria a role se não existir
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        
        // Método para criar os usuários padrão
        private static async Task CreateUsers(UserManager<ApplicationUser> userManager)
        {
            // Lista de usuários pré-configurados
            var usuarios = new[]
            {
                new { UserName = "administrador", Email = "admin@ordemservico.com", NomeLoja = "Administrador", Senha = "qActive4", IsAdmin = true },
                new { UserName = "loja1", Email = "loja1@ordemservico.com", NomeLoja = "LOJA 1 | Matriz", Senha = "loja1q", IsAdmin = false },
                new { UserName = "loja2", Email = "loja2@ordemservico.com", NomeLoja = "LOJA 2 | NORTE", Senha = "loja2q", IsAdmin = false },
                new { UserName = "loja3", Email = "loja3@ordemservico.com", NomeLoja = "LOJA 3 | CD", Senha = "loja3q", IsAdmin = false },
                new { UserName = "loja4", Email = "loja4@ordemservico.com", NomeLoja = "LOJA 4 | SUL", Senha = "loja4q", IsAdmin = false },
                new { UserName = "loja5", Email = "loja5@ordemservico.com", NomeLoja = "LOJA 5 | PORTO", Senha = "loja5q", IsAdmin = false },
                new { UserName = "loja6", Email = "loja6@ordemservico.com", NomeLoja = "LOJA 6 | GURUPI CENTRO", Senha = "loja6q", IsAdmin = false },
                new { UserName = "loja8", Email = "loja8@ordemservico.com", NomeLoja = "LOJA 8 | EMPÓRIO", Senha = "loja8q", IsAdmin = false },
                new { UserName = "loja9", Email = "loja9@ordemservico.com", NomeLoja = "LOJA 9 | JARDIM PAULISTA", Senha = "loja9q", IsAdmin = false },
                new { UserName = "loja10", Email = "loja10@ordemservico.com", NomeLoja = "LOJA 10 | JARDIM AMÉRICA", Senha = "loja10q", IsAdmin = false },
                new { UserName = "loja12", Email = "loja12@ordemservico.com", NomeLoja = "LOJA 12 | POUSO ALEGRE", Senha = "loja12q", IsAdmin = false },
                new { UserName = "loja13", Email = "loja13@ordemservico.com", NomeLoja = "LOJA 13 | PARAISO CENTRO", Senha = "loja13q", IsAdmin = false },
                new { UserName = "loja14", Email = "loja14@ordemservico.com", NomeLoja = "LOJA 14 | RESTAURANTE", Senha = "loja14q", IsAdmin = false },
                new { UserName = "loja15", Email = "loja15@ordemservico.com", NomeLoja = "LOJA 15 | GURUPI ABV", Senha = "loja15q", IsAdmin = false },
                new { UserName = "loja16", Email = "loja16@ordemservico.com", NomeLoja = "LOJA 16 | BERTAVILLE", Senha = "loja16q", IsAdmin = false },
                // Técnicos
                new { UserName = "odailton", Email = "odailton@ordemservico.com", NomeLoja = "Odailton", Senha = "tecnico123", IsAdmin = false },
                new { UserName = "pedrokstro", Email = "pedrokstro@ordemservico.com", NomeLoja = "Pedro Kstro", Senha = "tecnico123", IsAdmin = false },
                new { UserName = "pedroeduardo", Email = "pedroeduardo@ordemservico.com", NomeLoja = "Pedro Eduardo", Senha = "tecnico123", IsAdmin = false },
                new { UserName = "ronielson", Email = "ronielson@ordemservico.com", NomeLoja = "Ronielson", Senha = "tecnico123", IsAdmin = false }
            };
            
            // Cria cada usuário se não existir
            foreach (var userData in usuarios)
            {
                var user = await userManager.FindByNameAsync(userData.UserName);
                if (user == null)
                {
                    // Cria o novo usuário
                    user = new ApplicationUser
                    {
                        UserName = userData.UserName,
                        Email = userData.Email,
                        NomeLoja = userData.NomeLoja,
                        IsAdmin = userData.IsAdmin,
                        EmailConfirmed = true
                    };
                    
                    // Cria o usuário com a senha
                    var result = await userManager.CreateAsync(user, userData.Senha);
                    
                    if (result.Succeeded)
                    {
                        // Atribui a role apropriada
                        if (userData.IsAdmin)
                        {
                            await userManager.AddToRoleAsync(user, "Admin");
                        }
                        else
                        {
                            await userManager.AddToRoleAsync(user, "User");
                        }
                    }
                }
            }
        }
    }
}