namespace KleioHistoria.API.Models.Entities;

/// <summary>
/// Artigo histórico. Tanto a publicação manual quanto a importação de PDF
/// resultam nesta mesma entidade, com seus <see cref="Capitulos"/>.
/// </summary>
public class Artigo
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>Identificador amigável e único usado na URL (ex.: revolucao-francesa).</summary>
    public string Slug { get; set; } = string.Empty;

    public string Resumo { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    /// <summary>URL da imagem de capa: upload local (ex.: /uploads/imagens/abc.jpg) ou link http(s).</summary>
    public string? ImagemCapa { get; set; }

    /// <summary>
    /// Imagem larga opcional para o carrossel de destaques (upload local ou link).
    /// Sem ela, o carrossel usa a <see cref="ImagemCapa"/>.
    /// </summary>
    public string? ImagemBanner { get; set; }

    /// <summary>Autor exibido na leitura, quando informado.</summary>
    public string? Autor { get; set; }

    /// <summary>Se verdadeiro, o artigo aparece no carrossel da página inicial.</summary>
    public bool Destaque { get; set; }

    public StatusArtigo Status { get; set; } = StatusArtigo.Rascunho;

    /// <summary>Calculado automaticamente a partir do texto dos capítulos (200 palavras/min).</summary>
    public int TempoLeituraMinutos { get; set; }

    /// <summary>Nome do PDF de origem (guardado no volume de uploads), quando importado.</summary>
    public string? ArquivoPdfOrigem { get; set; }

    public DateTime? DataPublicacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public List<Capitulo> Capitulos { get; set; } = new();
}
