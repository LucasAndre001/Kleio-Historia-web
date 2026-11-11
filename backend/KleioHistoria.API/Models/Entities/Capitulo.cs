namespace KleioHistoria.API.Models.Entities;

/// <summary>Capítulo de um artigo. A leitura respeita o campo <see cref="Ordem"/>.</summary>
public class Capitulo
{
    public int Id { get; set; }

    public int ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    /// <summary>Posição do capítulo no artigo, começando em 1.</summary>
    public int Ordem { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>Conteúdo em HTML já sanitizado (sem scripts ou atributos perigosos).</summary>
    public string ConteudoHtml { get; set; } = string.Empty;

    /// <summary>
    /// Versão em texto puro do conteúdo, gerada automaticamente.
    /// Usada na pesquisa por palavras-chave e no cálculo do tempo de leitura,
    /// evitando que a busca encontre nomes de tags HTML.
    /// </summary>
    public string ConteudoTexto { get; set; } = string.Empty;

    public List<Imagem> Imagens { get; set; } = new();
}
