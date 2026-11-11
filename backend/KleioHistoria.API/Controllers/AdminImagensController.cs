using KleioHistoria.API.Dtos.Imagens;
using KleioHistoria.API.Infraestrutura;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KleioHistoria.API.Controllers;

[ApiController]
[Route("api/admin/imagens")]
[Authorize(Roles = Perfis.Admin)]
[Produces("application/json")]
public class AdminImagensController(IImagemService imagemService) : ControllerBase
{
    /// <summary>
    /// Recebe uma imagem (multipart/form-data, campo "arquivo") e devolve a URL local,
    /// usada no editor de capítulos ou como imagem de capa.
    /// </summary>
    /// <response code="201">Imagem salva.</response>
    /// <response code="413">Imagem maior que o limite.</response>
    /// <response code="415">Formato não suportado.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitesUpload.CorpoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesUpload.CorpoMaximoBytes)]
    [ProducesResponseType<ImagemUploadRespostaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<ImagemUploadRespostaDto>> Enviar(IFormFile arquivo, CancellationToken ct)
    {
        var resposta = await imagemService.SalvarAsync(arquivo, ct);
        return Created(resposta.Url, resposta);
    }
}
