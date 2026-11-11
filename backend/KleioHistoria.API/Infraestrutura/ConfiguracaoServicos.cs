using System.Text;
using System.Threading.RateLimiting;
using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Data;
using KleioHistoria.API.Models.Entities;
using KleioHistoria.API.Services;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace KleioHistoria.API.Infraestrutura;

/// <summary>
/// Registro dos serviços no contêiner de injeção de dependência,
/// separado por assunto para deixar o Program.cs curto e legível.
/// </summary>
public static class ConfiguracaoServicos
{
    /// <summary>Lê e VALIDA as configurações já na inicialização (falha rápida com mensagem clara).</summary>
    public static IServiceCollection AddKleioOpcoes(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<JwtOpcoes>().Bind(config.GetSection(JwtOpcoes.Secao)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AdminOpcoes>().Bind(config.GetSection(AdminOpcoes.Secao)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ArmazenamentoOpcoes>().Bind(config.GetSection(ArmazenamentoOpcoes.Secao));
        return services;
    }

    public static IServiceCollection AddKleioBancoDeDados(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Padrao");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'Padrao' não configurada. Defina ConnectionStrings__Padrao (veja o .env.example).");

        services.AddDbContext<AppDbContext>(opcoes => opcoes
            .UseNpgsql(connectionString, npgsql =>
                // Repete automaticamente operações que falharem por instabilidade momentânea.
                npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            // Se o snapshot das migrations divergir do modelo, registra um aviso no log
            // (em vez de impedir a API de iniciar). Gere uma nova migration nesse caso.
            .ConfigureWarnings(avisos => avisos.Log(RelationalEventId.PendingModelChangesWarning)));

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgresql");
        return services;
    }

    /// <summary>
    /// ASP.NET Core Identity "enxuto" (AddIdentityCore): só gerenciamento de usuários,
    /// perfis e senhas — sem cookies nem páginas prontas, pois usamos JWT.
    /// </summary>
    public static IServiceCollection AddKleioIdentidade(this IServiceCollection services)
    {
        services.AddIdentityCore<Usuario>(opcoes =>
            {
                opcoes.User.RequireUniqueEmail = true;
                opcoes.Password.RequiredLength = 8;
                opcoes.Password.RequireDigit = true;
                opcoes.Password.RequireLowercase = true;
                opcoes.Password.RequireUppercase = true;
                opcoes.Password.RequireNonAlphanumeric = false;
                opcoes.Lockout.AllowedForNewUsers = true;
                opcoes.Lockout.MaxFailedAccessAttempts = 5;
                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }

    public static IServiceCollection AddKleioAutenticacaoJwt(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection(JwtOpcoes.Secao).Get<JwtOpcoes>() ?? new JwtOpcoes();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                // Mantém os nomes curtos das claims ("sub", "role") como estão no token.
                opcoes.MapInboundClaims = false;
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Se a chave estiver ausente, ValidateOnStart (AddKleioOpcoes) interrompe a API antes.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave.PadRight(32))),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddKleioServicos(this IServiceCollection services)
    {
        // Scoped = uma instância por requisição HTTP (mesmo ciclo de vida do DbContext).
        services.AddScoped<IArtigoService, ArtigoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISugestaoService, SugestaoService>();
        services.AddScoped<IImagemService, ImagemService>();
        services.AddScoped<IPdfExtracaoService, PdfExtracaoService>();

        // Singleton = uma instância para toda a aplicação (sem estado por requisição).
        services.AddSingleton<IConteudoHtmlService, ConteudoHtmlService>();
        return services;
    }

    /// <summary>Swagger com botão "Authorize" para testar os endpoints protegidos com o JWT.</summary>
    public static IServiceCollection AddKleioSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Kleio História API",
                Version = "v1",
                Description = "API REST da plataforma acadêmica Kleio História (execução local)."
            });

            opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Faça login em POST /api/auth/login e cole aqui apenas o token."
            });

            opcoes.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
            });

            var xml = Path.Combine(AppContext.BaseDirectory, "KleioHistoria.API.xml");
            if (File.Exists(xml))
                opcoes.IncludeXmlComments(xml);
        });
        return services;
    }

    /// <summary>
    /// CORS só é necessário quando o Angular roda fora do Docker (ng serve na porta 4200
    /// sem proxy). No Docker, o Nginx do frontend encaminha /api para a API (mesma origem).
    /// </summary>
    public static IServiceCollection AddKleioCors(this IServiceCollection services, IConfiguration config)
    {
        var origens = config.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? ["http://localhost:4200"];
        services.AddCors(opcoes => opcoes.AddPolicy("frontend", politica =>
            politica.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));
        return services;
    }

    /// <summary>
    /// Limita tentativas de login e envio de sugestões por endereço IP,
    /// dificultando ataques de força bruta e spam.
    /// </summary>
    public static IServiceCollection AddKleioLimiteRequisicoes(this IServiceCollection services)
    {
        services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opcoes.AddPolicy(PoliticasLimite.Login, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            opcoes.AddPolicy(PoliticasLimite.Sugestoes, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
        });

        // Atrás do Nginx, o IP real do visitante chega no cabeçalho X-Forwarded-For.
        services.Configure<ForwardedHeadersOptions>(opcoes =>
        {
            opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Rede interna do Docker: confiamos no proxy do próprio compose.
            opcoes.KnownIPNetworks.Clear();
            opcoes.KnownProxies.Clear();
        });

        return services;
    }
}
