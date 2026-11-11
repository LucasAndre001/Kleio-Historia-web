using KleioHistoria.API.Dtos.Artigos;
using KleioHistoria.API.Dtos.Pdf;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KleioHistoria.API.Controllers;

/// <summary>
/// Gestão de conteúdo. [Authorize(Roles = "Admin")] faz o ASP.NET exigir um JWT
/// válido com o perfil Admin em TODOS os endpoints desta classe.
/// </summary>
[ApiController]
[Route("api/admin/artigos")]
[Authorize(Roles = Perfis.Admin)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminArtigosController(IArtigoService artigoService, IPdfExtracaoService pdfService) : ControllerBase
{
    /// <summary>Lista todos os artigos, incluindo rascunhos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArtigoAdminResumoDto>>> Listar(CancellationToken ct)
        => Ok(await artigoService.ListarTodosAsync(ct));

    /// <summary>Artigo completo para edição ou pré-visualização.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ArtigoAdminDetalheDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtigoAdminDetalheDto>> Obter(int id, CancellationToken ct)
        => Ok(await artigoService.ObterPorIdAsync(id, ct));

    /// <summary>Cria um artigo (sempre como rascunho).</summary>
    [HttpPost]
    [ProducesResponseType<ArtigoAdminDetalheDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArtigoAdminDetalheDto>> Criar(ArtigoSalvarDto dto, CancellationToken ct)
    {
        var criado = await artigoService.CriarAsync(dto, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    /// <summary>Atualiza dados e capítulos de um artigo.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ArtigoAdminDetalheDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtigoAdminDetalheDto>> Atualizar(int id, ArtigoSalvarDto dto, CancellationToken ct)
        => Ok(await artigoService.AtualizarAsync(id, dto, ct));

    /// <summary>Exclui definitivamente o artigo, seus capítulos e registros de imagem.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await artigoService.ExcluirAsync(id, ct);
        return NoContent();
    }

    /// <summary>Publica o artigo (passa a aparecer na área pública).</summary>
    [HttpPost("{id:int}/publicar")]
    [ProducesResponseType<ArtigoAdminDetalheDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArtigoAdminDetalheDto>> Publicar(int id, CancellationToken ct)
        => Ok(await artigoService.PublicarAsync(id, ct));

    /// <summary>Retorna o artigo para rascunho, sem excluí-lo.</summary>
    [HttpPost("{id:int}/despublicar")]
    [ProducesResponseType<ArtigoAdminDetalheDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArtigoAdminDetalheDto>> Despublicar(int id, CancellationToken ct)
        => Ok(await artigoService.DespublicarAsync(id, ct));

    /// <summary>Liga ou desliga o destaque do artigo no carrossel da página inicial.</summary>
    [HttpPut("{id:int}/destaque")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DefinirDestaque(int id, DestaqueDto dto, CancellationToken ct)
    {
        await artigoService.DefinirDestaqueAsync(id, dto.Destaque, ct);
        return NoContent();
    }

    /// <summary>
    /// Recebe um PDF (multipart/form-data, campo "arquivo") e devolve o texto extraído
    /// e organizado em capítulos sugeridos. NADA é salvo como artigo: o professor
    /// revisa no editor e só então salva.
    /// </summary>
    /// <response code="415">Arquivo não é PDF.</response>
    /// <response code="422">PDF corrompido, protegido ou sem texto selecionável (digitalizado).</response>
    [HttpPost("importar-pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitesUpload.CorpoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesUpload.CorpoMaximoBytes)]
    [ProducesResponseType<PdfImportacaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PdfImportacaoDto>> ImportarPdf(IFormFile arquivo, CancellationToken ct)
        => Ok(await pdfService.ImportarAsync(arquivo, ct));
}
