using System.ComponentModel.DataAnnotations;

namespace KleioHistoria.API.Dtos.Capitulos;

/// <summary>Capítulo devolvido pela API, já em ordem.</summary>
public record CapituloDto(
    int Id,
    int Ordem,
    string Titulo,
    string ConteudoHtml,
    IReadOnlyList<ImagemCapituloDto> Imagens);

/// <summary>Imagem extraída do conteúdo do capítulo.</summary>
public record ImagemCapituloDto(int Id, string Url, string? Legenda, string? TextoAlternativo);

/// <summary>Capítulo enviado pelo editor.</summary>
public class CapituloSalvarDto
{
    [Required(ErrorMessage = "Todo capítulo precisa de um título.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "O título do capítulo deve ter até 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>HTML produzido pelo editor. Será sanitizado antes de ser salvo.</summary>
    [StringLength(1_000_000, ErrorMessage = "O conteúdo do capítulo é grande demais.")]
    public string ConteudoHtml { get; set; } = string.Empty;
}
