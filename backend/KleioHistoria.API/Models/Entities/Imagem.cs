namespace KleioHistoria.API.Models.Entities;

/// <summary>
/// Imagem inserida no conteúdo de um capítulo.
/// Os registros são extraídos automaticamente do HTML (&lt;figure&gt;/&lt;img&gt;)
/// sempre que o capítulo é salvo, mantendo a tabela coerente com o texto.
/// A imagem de capa fica no próprio <see cref="Artigo.ImagemCapa"/>.
/// </summary>
public class Imagem
{
    public int Id { get; set; }

    public int CapituloId { get; set; }
    public Capitulo Capitulo { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? Legenda { get; set; }
    public string? TextoAlternativo { get; set; }

    /// <summary>Posição da imagem dentro do capítulo.</summary>
    public int Ordem { get; set; }
}
