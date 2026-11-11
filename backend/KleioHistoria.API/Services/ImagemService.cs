using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Dtos.Imagens;
using KleioHistoria.API.Excecoes;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KleioHistoria.API.Services;

/// <summary>
/// Upload de imagens para o editor e para a capa.
/// Os arquivos ficam em {CaminhoRaiz}/imagens (volume Docker) e são servidos
/// como estáticos em /uploads/imagens/{nome}.
/// </summary>
public class ImagemService(IOptions<ArmazenamentoOpcoes> opcoes, ILogger<ImagemService> logger) : IImagemService
{
    private static readonly HashSet<string> ExtensoesAceitas =
        new([".jpg", ".jpeg", ".png", ".gif", ".webp"], StringComparer.OrdinalIgnoreCase);

    public async Task<ImagemUploadRespostaDto> SalvarAsync(IFormFile arquivo, CancellationToken ct = default)
    {
        var limiteBytes = opcoes.Value.TamanhoMaximoImagemMb * 1024L * 1024L;

        if (arquivo is null || arquivo.Length == 0)
            throw new ArquivoInvalidoExcecao("Nenhuma imagem foi enviada.", StatusCodes.Status400BadRequest);

        if (arquivo.Length > limiteBytes)
            throw new ArquivoInvalidoExcecao(
                $"A imagem excede o limite de {opcoes.Value.TamanhoMaximoImagemMb} MB.",
                StatusCodes.Status413PayloadTooLarge);

        // 1ª barreira: extensão do nome do arquivo.
        var extensaoInformada = Path.GetExtension(arquivo.FileName);
        if (!ExtensoesAceitas.Contains(extensaoInformada))
            throw new ArquivoInvalidoExcecao(
                "Formato não suportado. Envie JPG, PNG, GIF ou WEBP.",
                StatusCodes.Status415UnsupportedMediaType);

        // 2ª barreira: Content-Type declarado pelo navegador.
        var tipoDeclarado = arquivo.ContentType?.ToLowerInvariant();
        if (tipoDeclarado != "image/jpg" && !ValidadorArquivo.TiposImagem.Values.Contains(tipoDeclarado))
            throw new ArquivoInvalidoExcecao("Tipo de arquivo (MIME) não suportado.", StatusCodes.Status415UnsupportedMediaType);

        // 3ª barreira (a mais importante): a assinatura binária do conteúdo.
        await using var entrada = arquivo.OpenReadStream();
        var cabecalho = await ValidadorArquivo.LerCabecalhoAsync(entrada, 12, ct);
        var extensaoReal = ValidadorArquivo.DetectarImagem(cabecalho)
            ?? throw new ArquivoInvalidoExcecao("O conteúdo do arquivo não corresponde a uma imagem válida.");

        // O nome é gerado pelo servidor: nunca usamos o nome enviado,
        // o que evita sobrescrita e caminhos maliciosos ("../").
        var nomeArquivo = $"{Guid.NewGuid():N}{extensaoReal}";
        var pasta = Path.Combine(Path.GetFullPath(opcoes.Value.CaminhoRaiz), ArmazenamentoOpcoes.PastaImagens);
        Directory.CreateDirectory(pasta);
        var destino = Path.Combine(pasta, nomeArquivo);

        await using (var saida = new FileStream(destino, FileMode.CreateNew, FileAccess.Write))
        {
            await saida.WriteAsync(cabecalho, ct);   // bytes já lidos para a validação
            await entrada.CopyToAsync(saida, ct);    // restante do arquivo
        }

        logger.LogInformation("Imagem salva: {Arquivo} ({Bytes} bytes)", nomeArquivo, arquivo.Length);
        return new ImagemUploadRespostaDto(
            $"{ArmazenamentoOpcoes.UrlPublicaImagens}/{nomeArquivo}", nomeArquivo, arquivo.Length);
    }
}
