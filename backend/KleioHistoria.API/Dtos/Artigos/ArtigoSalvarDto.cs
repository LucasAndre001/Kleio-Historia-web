using System.ComponentModel.DataAnnotations;
using KleioHistoria.API.Dtos.Capitulos;

namespace KleioHistoria.API.Dtos.Artigos;

/// <summary>
/// Dados enviados pelo editor para criar ou atualizar um artigo.
/// O mesmo DTO atende a publicação manual e a importação de PDF.
/// A ordem dos capítulos é a ordem da lista.
///
/// As anotações [Required], [StringLength]... são verificadas automaticamente
/// pelo [ApiController]: se falharem, a API responde 400 antes de chegar ao Service.
/// </summary>
public class ArtigoSalvarDto
{
    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(600, ErrorMessage = "O resumo deve ter no máximo 600 caracteres.")]
    public string? Resumo { get; set; }

    [Required(ErrorMessage = "Informe a categoria.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "A categoria deve ter entre 2 e 80 caracteres.")]
    public string Categoria { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImagemCapa { get; set; }

    /// <summary>Imagem do carrossel de destaques (opcional).</summary>
    [StringLength(500)]
    public string? ImagemBanner { get; set; }

    [StringLength(150)]
    public string? Autor { get; set; }

    /// <summary>Opcional. Se vazio, o slug é gerado a partir do título.</summary>
    [StringLength(200)]
    public string? Slug { get; set; }

    public bool Destaque { get; set; }

    /// <summary>Identificador devolvido pela importação de PDF (quando houver).</summary>
    [StringLength(260)]
    public string? ArquivoPdfOrigem { get; set; }

    [MaxLength(100, ErrorMessage = "Um artigo pode ter no máximo 100 capítulos.")]
    public List<CapituloSalvarDto> Capitulos { get; set; } = new();
}

/// <summary>Filtros aceitos por GET /api/artigos.</summary>
public class FiltroArtigosDto
{
    /// <summary>Texto livre: busca no título, resumo, categoria e conteúdo dos capítulos.</summary>
    [StringLength(100)]
    public string? Busca { get; set; }

    [StringLength(80)]
    public string? Categoria { get; set; }

    /// <summary>recentes (padrão) | antigos | titulo</summary>
    public string? Ordem { get; set; }

    /// <summary>Se verdadeiro, retorna apenas artigos em destaque (carrossel).</summary>
    public bool? Destaque { get; set; }
}

/// <summary>Corpo de PUT /api/admin/artigos/{id}/destaque.</summary>
public record DestaqueDto(bool Destaque);
