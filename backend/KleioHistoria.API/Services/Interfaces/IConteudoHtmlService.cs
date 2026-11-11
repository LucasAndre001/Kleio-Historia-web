namespace KleioHistoria.API.Services.Interfaces;

/// <summary>Imagem encontrada dentro do HTML de um capítulo.</summary>
public record ImagemEncontrada(string Url, string? Legenda, string? TextoAlternativo);

/// <summary>Tratamento do HTML produzido pelo editor ou pela importação de PDF.</summary>
public interface IConteudoHtmlService
{
    /// <summary>Remove scripts, eventos (onclick...), estilos e qualquer tag fora da lista permitida.</summary>
    string Sanitizar(string? html);

    /// <summary>Texto puro do HTML (para busca e tempo de leitura).</summary>
    string ExtrairTexto(string html);

    /// <summary>Lista as imagens (&lt;img&gt;) com a legenda do &lt;figcaption&gt; correspondente.</summary>
    IReadOnlyList<ImagemEncontrada> ExtrairImagens(string html);

    /// <summary>Aceita apenas URLs locais (/uploads/...) ou http/https.</summary>
    bool EhUrlSegura(string? url);
}
