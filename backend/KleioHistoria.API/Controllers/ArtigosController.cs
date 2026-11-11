using KleioHistoria.API.Dtos.Artigos;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KleioHistoria.API.Controllers;

/// <summary>Endpoints públicos de leitura. Retornam somente artigos publicados.</summary>
[ApiController]
[Route("api/artigos")]
[Produces("application/json")]
public class ArtigosController(IArtigoService artigoService) : ControllerBase
{
    /// <summary>Lista artigos publicados, com pesquisa, filtro por categoria e ordenação.</summary>
    /// <param name="filtro">busca, categoria, ordem (recentes|antigos|titulo), destaque</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ArtigoResumoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArtigoResumoDto>>> Listar([FromQuery] FiltroArtigosDto filtro, CancellationToken ct)
        => Ok(await artigoService.ListarPublicadosAsync(filtro, ct));

    /// <summary>Categorias que possuem ao menos um artigo publicado.</summary>
    [HttpGet("categorias")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> Categorias(CancellationToken ct)
        => Ok(await artigoService.ListarCategoriasAsync(ct));

    /// <summary>Artigo completo (capítulos, imagens e HTML) pelo slug.</summary>
    /// <response code="404">Slug inexistente ou artigo em rascunho.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType<ArtigoDetalheDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtigoDetalheDto>> ObterPorSlug(string slug, CancellationToken ct)
        => Ok(await artigoService.ObterPublicadoPorSlugAsync(slug, ct));
}
