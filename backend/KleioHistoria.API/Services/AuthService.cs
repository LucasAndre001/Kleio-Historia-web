using System.Security.Claims;
using System.Text;
using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Dtos.Auth;
using KleioHistoria.API.Models.Entities;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KleioHistoria.API.Services;

/// <summary>
/// Autenticação do administrador.
///
/// 1. O <see cref="UserManager{TUser}"/> do Identity confere a senha contra o hash
///    guardado no banco (PBKDF2 com salt — a senha original nunca é armazenada).
/// 2. Após 5 tentativas erradas a conta fica bloqueada por alguns minutos (lockout).
/// 3. Com a senha correta, emitimos um JWT assinado com HMAC-SHA256.
///    O frontend envia esse token no cabeçalho "Authorization: Bearer ...".
/// </summary>
public class AuthService(
    UserManager<Usuario> userManager,
    IOptions<JwtOpcoes> jwtOpcoes,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOpcoes _jwt = jwtOpcoes.Value;

    public async Task<ResultadoLogin> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var usuario = await userManager.FindByEmailAsync(dto.Email.Trim());

        // Mesma resposta para "e-mail inexistente" e "senha errada":
        // não revelamos quais e-mails estão cadastrados.
        if (usuario is null)
            return new ResultadoLogin(SituacaoLogin.CredenciaisInvalidas);

        if (await userManager.IsLockedOutAsync(usuario))
        {
            logger.LogWarning("Tentativa de login em conta bloqueada (usuário {Id})", usuario.Id);
            return new ResultadoLogin(SituacaoLogin.Bloqueado);
        }

        if (!await userManager.CheckPasswordAsync(usuario, dto.Senha))
        {
            await userManager.AccessFailedAsync(usuario); // conta para o lockout
            logger.LogWarning("Senha incorreta para o usuário {Id}", usuario.Id); // nunca logar a senha
            return new ResultadoLogin(SituacaoLogin.CredenciaisInvalidas);
        }

        await userManager.ResetAccessFailedCountAsync(usuario);

        var perfis = (await userManager.GetRolesAsync(usuario)).ToList();
        var (token, expiraEm) = GerarToken(usuario, perfis);

        logger.LogInformation("Login realizado (usuário {Id})", usuario.Id);
        return new ResultadoLogin(
            SituacaoLogin.Sucesso,
            new LoginRespostaDto(token, expiraEm,
                new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email ?? string.Empty, perfis)));
    }

    private (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario, IEnumerable<string> perfis)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_jwt.ExpiracaoMinutos);

        // Claims = informações gravadas dentro do token.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new("name", usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(perfis.Select(p => new Claim("role", p)));

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Chave));

        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Emissor,
            Audience = _jwt.Audiencia,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);
        return (token, expiraEm);
    }
}
