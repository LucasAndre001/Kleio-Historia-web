using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KleioHistoria.API.Data;

/// <summary>
/// Executado uma vez, antes de a API começar a responder:
/// 1. aguarda o PostgreSQL aceitar conexões (com novas tentativas);
/// 2. aplica as migrations pendentes (cria ou atualiza o esquema, sem apagar dados);
/// 3. garante o perfil "Admin" e o usuário administrador de demonstração;
/// 4. na primeira inicialização, insere artigos demonstrativos.
/// </summary>
public static class DbInicializador
{
    private const int TentativasMaximas = 30;
    private static readonly TimeSpan IntervaloEntreTentativas = TimeSpan.FromSeconds(2);

    public static async Task InicializarAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var escopo = services.CreateScope();
        var sp = escopo.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kleio.Inicializacao");
        var db = sp.GetRequiredService<AppDbContext>();

        await AplicarMigrationsAsync(db, logger, ct);
        var (admin, adminRecemCriado) = await GarantirAdministradorAsync(sp, logger);

        // Os dados demonstrativos entram só junto com o primeiro administrador.
        // Assim, se o professor excluir todos os artigos, eles não "voltam" ao reiniciar.
        var armazenamento = sp.GetRequiredService<IOptions<ArmazenamentoOpcoes>>().Value;
        if (armazenamento.SemearDadosDemonstrativos && adminRecemCriado && !await db.Artigos.AnyAsync(ct))
            await SeedDemonstrativo.SemearAsync(sp, admin.Id, logger, ct);
    }

    private static async Task AplicarMigrationsAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                var pendentes = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
                if (pendentes.Count > 0)
                {
                    logger.LogInformation("Aplicando {Quantidade} migration(s): {Nomes}", pendentes.Count, string.Join(", ", pendentes));
                    await db.Database.MigrateAsync(ct);
                }
                else
                {
                    logger.LogInformation("Banco de dados atualizado; nenhuma migration pendente.");
                }
                return;
            }
            catch (Exception ex) when (tentativa < TentativasMaximas && ex is not OperationCanceledException)
            {
                // Comum no primeiro "docker compose up": o PostgreSQL ainda está iniciando.
                logger.LogWarning("Banco indisponível (tentativa {Tentativa}/{Max}): {Erro}. Nova tentativa em {Segundos}s...",
                    tentativa, TentativasMaximas, ex.GetBaseException().Message, IntervaloEntreTentativas.TotalSeconds);
                await Task.Delay(IntervaloEntreTentativas, ct);
            }
        }
    }

    private static async Task<(Usuario Admin, bool RecemCriado)> GarantirAdministradorAsync(IServiceProvider sp, ILogger logger)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = sp.GetRequiredService<UserManager<Usuario>>();
        var opcoes = sp.GetRequiredService<IOptions<AdminOpcoes>>().Value;

        if (!await roleManager.RoleExistsAsync(Perfis.Admin))
            Garantir(await roleManager.CreateAsync(new IdentityRole<int>(Perfis.Admin)), "criar o perfil Admin");

        var recemCriado = false;
        var usuario = await userManager.FindByEmailAsync(opcoes.Email);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                UserName = opcoes.Email,
                Email = opcoes.Email,
                EmailConfirmed = true,
                Nome = opcoes.Nome
            };
            // CreateAsync calcula o hash da senha; a senha em texto puro não é gravada.
            Garantir(await userManager.CreateAsync(usuario, opcoes.Senha), "criar o administrador (verifique ADMIN_SENHA)");
            recemCriado = true;
            logger.LogInformation("Administrador de demonstração criado: {Email}", opcoes.Email);
        }

        if (!await userManager.IsInRoleAsync(usuario, Perfis.Admin))
            Garantir(await userManager.AddToRoleAsync(usuario, Perfis.Admin), "atribuir o perfil Admin");

        return (usuario, recemCriado);
    }

    private static void Garantir(IdentityResult resultado, string acao)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"Não foi possível {acao}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
    }
}
