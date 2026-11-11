using KleioHistoria.API.Dtos.Auth;

namespace KleioHistoria.API.Services.Interfaces;

public enum SituacaoLogin
{
    Sucesso,
    CredenciaisInvalidas,
    Bloqueado
}

public record ResultadoLogin(SituacaoLogin Situacao, LoginRespostaDto? Resposta = null);

public interface IAuthService
{
    Task<ResultadoLogin> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
