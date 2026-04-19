using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrdemServicoMVC.Data;
using OrdemServicoMVC.Models;
using OrdemServicoMVC.Hubs; // Importa o namespace dos Hubs
using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides; // Para configuração de proxy reverso
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configuração de logging para evitar problemas com EventLog
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuração para proxy reverso (necessário para hospedagem em nuvem)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Aceita headers de proxy reverso para hostname correto
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
    
    // Limpa redes conhecidas para aceitar qualquer proxy (necessário para runasp.net)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    
    // Configurações específicas para runasp.net
    options.ForwardLimit = null; // Remove limite de forwards
    options.RequireHeaderSymmetry = false; // Não requer simetria de headers
});

// Configuração da string de conexão
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordemservico.db";
var databaseProvider = builder.Configuration.GetConnectionString("DatabaseProvider") ?? "SQLite";



// Configuração do Entity Framework
// Suporta SQLite (padrão), MySQL e SQL Server — configurável via appsettings.json
if (databaseProvider == "MySQL")
{
    // Configuração para MySQL
    var mysqlConnection = builder.Configuration.GetConnectionString("MySQLConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection)));
}
else if (databaseProvider == "SqlServer")
{
    // Configuração para SQL Server (databaseasp.net)
    var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlServerConnection));
}
else
{
    // Configuração padrão para SQLite
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}

// Configuração das configurações da aplicação
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Persistência das chaves de Data Protection para evitar invalidação de tokens após reciclagens
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("COS-OrdemServico")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(180));

// Configuração do cache em memória para otimização de performance
builder.Services.AddMemoryCache(options =>
{
    // Define o tamanho máximo do cache (100MB)
    options.SizeLimit = 100 * 1024 * 1024;
    
    // Define o tempo de compactação do cache (remove itens expirados a cada 5 minutos)
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/javascript",
        "text/css",
        "text/html",
        "text/plain",
        "image/svg+xml",
        "application/xml",
        "font/woff2",
        "application/font-woff2"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal); // Melhor compressão para assets estáticos
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal);

builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 512; // 512 KB por resposta em cache
    options.UseCaseSensitivePaths = false;
});

// Registra serviços personalizados
builder.Services.AddScoped<OrdemServicoMVC.Services.ICacheService, OrdemServicoMVC.Services.CacheService>();
builder.Services.AddScoped<OrdemServicoMVC.Services.IPerformanceTestService, OrdemServicoMVC.Services.PerformanceTestService>();

// Configuração do Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configurações de senha
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    
    // Configurações de login
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configuração de cookies de autenticação para resolver problemas de timeout
builder.Services.ConfigureApplicationCookie(options =>
{
    // Define o tempo de expiração do cookie de autenticação (24 horas)
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    
    // Permite que o cookie seja renovado automaticamente quando o usuário está ativo
    options.SlidingExpiration = true;
    
    // Define a página de login para redirecionamento quando não autenticado
    options.LoginPath = "/Account/Login";
    
    // Define a página de acesso negado
    options.AccessDeniedPath = "/Account/AccessDenied";
    
    // Configurações de segurança do cookie
    options.Cookie.HttpOnly = true; // Previne acesso via JavaScript
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Usa HTTPS quando disponível
    options.Cookie.SameSite = SameSiteMode.Lax; // Proteção CSRF
    
    // Define o nome do cookie
    options.Cookie.Name = "OrdemServicoAuth";
    
    // Configuração para lidar com expiração de sessão
    options.Events.OnRedirectToLogin = context =>
    {
        // Se for uma requisição AJAX, retorna 401 ao invés de redirecionar
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // Para requisições normais, redireciona para login
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Configuração de localização para formato brasileiro
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Define a cultura padrão como português brasileiro
    var supportedCultures = new[] { new CultureInfo("pt-BR") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Adiciona serviços MVC
builder.Services.AddControllersWithViews();

// Configuração de CORS para resolver problemas de domínio em produção
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCorsPolicy", policy =>
    {
        // Permite requisições dos domínios de produção
        policy.WithOrigins(
            "https://quartettocontrolservice.runasp.net",
            "http://quartettocontrolservice.runasp.net",
            "https://quartettocontroledeordem.runasp.net",
            "http://quartettocontroledeordem.runasp.net"
        )
        .AllowAnyMethod() // Permite GET, POST, PUT, DELETE, etc.
        .AllowAnyHeader() // Permite qualquer header
        .AllowCredentials(); // Permite cookies de autenticação
    });
    
    // Política mais permissiva para desenvolvimento
    options.AddPolicy("DevelopmentCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Adiciona serviços do SignalR para comunicação em tempo real
builder.Services.AddSignalR();

// Configuração de autorização
builder.Services.AddAuthorization(options =>
{
    // Política para administradores
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    // Política para usuários autenticados
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "Admin"));
});

var app = builder.Build();

// Inicialização do banco de dados
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Aplica as migrations automaticamente
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        
        // Inicializa os dados padrão (comentado para evitar criação automática de usuários)
        // await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao inicializar o banco de dados.");
    }
}

// Configure the HTTP request pipeline.
// Configuração de forwarded headers (deve vir antes de outros middlewares)
app.UseForwardedHeaders();

// Configuração de tratamento de erros melhorada para produção
if (!app.Environment.IsDevelopment())
{
    // Middleware personalizado para capturar e logar erros detalhados
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            
            if (exceptionFeature != null)
            {
                // Log detalhado do erro para diagnóstico
                logger.LogError(exceptionFeature.Error, 
                    "Erro não tratado na aplicação. Path: {Path}, Method: {Method}, User: {User}, Host: {Host}",
                    context.Request.Path, 
                    context.Request.Method,
                    context.User?.Identity?.Name ?? "Anônimo",
                    context.Request.Host.ToString());
            }
            
            // Verifica se é um erro relacionado a host/domínio inválido
            if (context.Response.StatusCode == 400 || 
                exceptionFeature?.Error?.Message?.Contains("host", StringComparison.OrdinalIgnoreCase) == true)
            {
                logger.LogWarning("Possível erro de host/domínio inválido. Host: {Host}, UserAgent: {UserAgent}",
                    context.Request.Host.ToString(),
                    context.Request.Headers["User-Agent"].ToString());
            }
            
            // Redireciona para a página de erro
            context.Response.Redirect("/Home/Error");
        });
    });
    
    app.UseHsts();
}
else
{
    // Em desenvolvimento, mostra a página de exceção detalhada
    app.UseDeveloperExceptionPage();
}



app.UseHttpsRedirection();
app.UseResponseCompression();
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";
contentTypeProvider.Mappings[".webapp"] = "application/x-web-app-manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        // Arquivos versionados (com hash via asp-append-version) podem ter cache longo + immutable
        if (ctx.Context.Request.Query.ContainsKey("v") || ctx.Context.Request.QueryString.HasValue)
        {
            const int durationInSeconds = 60 * 60 * 24 * 365; // 1 ano
            ctx.Context.Response.Headers.CacheControl = $"public,max-age={durationInSeconds},immutable";
        }
        else
        {
            const int durationInSeconds = 60 * 60 * 24 * 7; // 7 dias
            ctx.Context.Response.Headers.CacheControl = $"public,max-age={durationInSeconds}";
        }
    }
});

// Configuração de CORS baseada no ambiente
if (app.Environment.IsDevelopment())
{
    // Em desenvolvimento, usa política mais permissiva
    app.UseCors("DevelopmentCorsPolicy");
}
else
{
    // Em produção, usa política restritiva para domínios específicos
    app.UseCors("ProductionCorsPolicy");
}

// Aplica a configuração de localização
app.UseRequestLocalization();

app.UseRouting();
app.UseResponseCaching();

// Configuração de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Mapeia o hub do SignalR para comunicação em tempo real
app.MapHub<ChatHub>("/mensagemHub");

// Configuração de rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
