using KleioHistoria.API.Dtos.Sugestoes;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KleioHistoria.API.Controllers;

/// <summary>Recebe sugestões de tema dos visitantes (sem autenticação).</summary>
[ApiController]
[Route("api/sugestoes")]
[Produces("application/json")]
public class SugestoesController(ISugestaoService sugestaoService) : ControllerBase
{
    /// <summary>Registra uma sugestão de novo artigo.</summary>
    /// <response code="201">Sugestão salva.</response>
    /// <response code="400">Campos inválidos.</response>
    /// <response code="429">Muitas sugestões enviadas em pouco tempo.</response>
    [HttpPost]
    [EnableRateLimiting(PoliticasLimite.Sugestoes)]
    [ProducesResponseType<SugestaoConfirmacaoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SugestaoConfirmacaoDto>> Criar(SugestaoCriarDto dto, CancellationToken ct)
    {
        var confirmacao = await sugestaoService.CriarAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, confirmacao);
    }
}
