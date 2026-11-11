using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using KleioHistoria.API.Services.Interfaces;

namespace KleioHistoria.API.Services;

/// <summary>
/// Sanitização e análise do HTML dos capítulos.
///
/// Usa a biblioteca HtmlSanitizer (que por sua vez usa o parser AngleSharp) com uma
/// LISTA BRANCA: só as tags e atributos abaixo sobrevivem. Qualquer &lt;script&gt;,
/// &lt;iframe&gt;, atributo on* ou URL "javascript:" é removido.
///
/// A classe é registrada como Singleton: o HtmlSanitizer é seguro para uso
/// simultâneo depois de configurado.
/// </summary>
public partial class ConteudoHtmlService : IConteudoHtmlService
{
    private static readonly string[] TagsPermitidas =
    [
        "p", "br", "strong", "b", "em", "i", "u", "s",
        "h2", "h3", "h4",
        "ul", "ol", "li",
        "blockquote", "hr",
        "a", "img", "figure", "figcaption",
        "code", "pre"
    ];

    private static readonly string[] AtributosPermitidos = ["href", "src", "alt", "title", "target", "rel"];

    private readonly HtmlSanitizer _sanitizador;
    private readonly HtmlParser _parser = new();

    public ConteudoHtmlService()
    {
        _sanitizador = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(TagsPermitidas, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(AtributosPermitidos, StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(["http", "https", "mailto"], StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(["href", "src"], StringComparer.OrdinalIgnoreCase),
            // Nenhum CSS inline é aceito: a aparência vem das folhas de estilo do site.
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>()
        });

        // Links sempre abrem em nova aba, sem dar acesso à janela de origem.
        _sanitizador.PostProcessNode += (_, e) =>
        {
            if (e.Node is IElement { TagName: "A" } link)
            {
                link.SetAttribute("target", "_blank");
                link.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string Sanitizar(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var limpo = _sanitizador.Sanitize(html).Trim();

        // Parágrafos vazios gerados pelo editor ("<p></p>") não acrescentam nada.
        return ParagrafoVazio().Replace(limpo, string.Empty).Trim();
    }

    public string ExtrairTexto(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Insere um espaço antes de cada tag de fechamento para que
        // "<p>fim</p><p>início</p>" não vire "fiminício".
        var documento = _parser.ParseDocument(html.Replace("</", " </"));
        var texto = documento.Body?.TextContent ?? string.Empty;
        return EspacosRepetidos().Replace(texto, " ").Trim();
    }

    public IReadOnlyList<ImagemEncontrada> ExtrairImagens(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var documento = _parser.ParseDocument(html);
        var imagens = new List<ImagemEncontrada>();

        foreach (var img in documento.QuerySelectorAll("img"))
        {
            var url = img.GetAttribute("src");
            if (!EhUrlSegura(url))
                continue;

            var legenda = img.Closest("figure")?.QuerySelector("figcaption")?.TextContent.Trim();
            var alt = img.GetAttribute("alt")?.Trim();

            imagens.Add(new ImagemEncontrada(
                url!,
                string.IsNullOrWhiteSpace(legenda) ? null : Limitar(legenda, 300),
                string.IsNullOrWhiteSpace(alt) ? null : Limitar(alt, 300)));
        }

        return imagens;
    }

    public bool EhUrlSegura(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Caminho local servido pela própria API (uploads e imagens demonstrativas).
        if (url.StartsWith("/uploads/", StringComparison.Ordinal) && !url.Contains(".."))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string Limitar(string texto, int max) => texto.Length <= max ? texto : texto[..max];

    [GeneratedRegex(@"<p>\s*(<br>)?\s*</p>")]
    private static partial Regex ParagrafoVazio();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();
}
