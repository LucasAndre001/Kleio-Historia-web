namespace KleioHistoria.API.Dtos.Pdf;

/// <summary>
/// Resultado da importação de PDF, devolvido para REVISÃO (nunca publicado
/// automaticamente). O frontend usa estes dados para preencher o editor.
/// <c>ArquivoId</c> é o nome interno do PDF salvo, enviado de volta em
/// <c>ArtigoSalvarDto.ArquivoPdfOrigem</c> ao gravar o artigo.
/// </summary>
public record PdfImportacaoDto(
    string ArquivoId,
    string NomeArquivoOriginal,
    int TotalPaginas,
    string? TituloSugerido,
    string? ResumoSugerido,
    IReadOnlyList<CapituloExtraidoDto> Capitulos,
    IReadOnlyList<PaginaExtraidaDto> Paginas,
    IReadOnlyList<string> Avisos);

/// <summary>Capítulo sugerido pela análise do PDF (títulos detectados pelo tamanho da fonte).</summary>
public record CapituloExtraidoDto(string Titulo, string ConteudoHtml);

/// <summary>Texto bruto de uma página, para o professor conferir a extração.</summary>
public record PaginaExtraidaDto(int Numero, string Texto);
