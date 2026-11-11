using KleioHistoria.API.Dtos.Imagens;

namespace KleioHistoria.API.Services.Interfaces;

public interface IImagemService
{
    /// <summary>Valida e grava a imagem no volume de uploads; retorna a URL local.</summary>
    Task<ImagemUploadRespostaDto> SalvarAsync(IFormFile arquivo, CancellationToken ct = default);
}
