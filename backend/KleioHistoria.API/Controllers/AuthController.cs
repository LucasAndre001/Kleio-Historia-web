using KleioHistoria.API.Dtos.Auth;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KleioHistoria.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Autentica o administrador e devolve um JWT.</summary>
    /// <response code="200">Credenciais válidas.</response>
    /// <response code="401">E-mail ou senha inválidos.</response>
    /// <response code="423">Conta bloqueada temporariamente após várias tentativas.</response>
    [HttpPost("login")]
    [EnableRateLimiting(PoliticasLimite.Login)]
    [ProducesResponseType<LoginRespostaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginRespostaDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var resultado = await authService.LoginAsync(dto, ct);

        return resultado.Situacao switch
        {
            SituacaoLogin.Sucesso => Ok(resultado.Resposta),
            SituacaoLogin.Bloqueado => Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Conta bloqueada",
                detail: "Muitas tentativas incorretas. Aguarde alguns minutos e tente novamente."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais inválidas",
                detail: "E-mail ou senha inválidos.")
        };
    }
}
