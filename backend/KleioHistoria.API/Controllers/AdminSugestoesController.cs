using KleioHistoria.API.Dtos.Sugestoes;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KleioHistoria.API.Controllers;

/// <summary>Leitura e exclusão das sugestões recebidas (painel do professor).</summary>
[ApiController]
[Route("api/admin/sugestoes")]
[Authorize(Roles = Perfis.Admin)]
[Produces("application/json")]
public class AdminSugestoesController(ISugestaoService sugestaoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SugestaoDto>>> Listar(CancellationToken ct)
        => Ok(await sugestaoService.ListarAsync(ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await sugestaoService.ExcluirAsync(id, ct);
        return NoContent();
    }
}
