using KleioHistoria.API.Dtos.Pdf;

namespace KleioHistoria.API.Services.Interfaces;

public interface IPdfExtracaoService
{
    /// <summary>
    /// Valida o arquivo, extrai o texto, guarda o PDF original no volume de uploads
    /// e devolve o conteúdo organizado em capítulos sugeridos, para revisão.
    /// </summary>
    Task<PdfImportacaoDto> ImportarAsync(IFormFile arquivo, CancellationToken ct = default);
}
