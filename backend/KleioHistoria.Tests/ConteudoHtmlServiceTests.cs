using KleioHistoria.API.Services;

namespace KleioHistoria.Tests;

/// <summary>Garante que o HTML salvo nunca carrega conteúdo executável.</summary>
public class ConteudoHtmlServiceTests
{
    private readonly ConteudoHtmlService _servico = new();

    [Fact]
    public void Sanitizar_RemoveScripts()
    {
        var html = _servico.Sanitizar("<p>Texto</p><script>alert('x')</script>");

        Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Texto</p>", html);
    }

    [Fact]
    public void Sanitizar_RemoveEventosEJavascriptEmLinks()
    {
        var html = _servico.Sanitizar(
            "<p onclick=\"roubar()\">Oi</p><a href=\"javascript:alert(1)\">link</a><img src=x onerror=alert(1)>");

        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitizar_RemoveEstilosEIframes()
    {
        var html = _servico.Sanitizar("<p style=\"position:fixed\">a</p><iframe src=\"https://exemplo.com\"></iframe>");

        Assert.DoesNotContain("style", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iframe", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitizar_MantemFormatacaoPermitida()
    {
        var entrada = "<h2>Título</h2><p><strong>negrito</strong> <em>itálico</em> <u>sublinhado</u></p>" +
                      "<ul><li>item</li></ul><blockquote><p>citação</p></blockquote><hr>" +
                      "<figure><img src=\"/uploads/imagens/a.jpg\" alt=\"Mapa\"><figcaption>Legenda</figcaption></figure>";

        var html = _servico.Sanitizar(entrada);

        foreach (var tag in new[] { "<h2>", "<strong>", "<em>", "<u>", "<ul>", "<blockquote>", "<hr>", "<figure>", "<figcaption>" })
            Assert.Contains(tag, html);
        Assert.Contains("src=\"/uploads/imagens/a.jpg\"", html);
    }

    [Fact]
    public void Sanitizar_LinksAbremEmNovaAbaComSeguranca()
    {
        var html = _servico.Sanitizar("<p><a href=\"https://exemplo.com\">fonte</a></p>");

        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
    }

    [Fact]
    public void ExtrairTexto_SeparaParagrafos()
    {
        var texto = _servico.ExtrairTexto("<p>fim</p><p>início</p>");
        Assert.Equal("fim início", texto);
    }

    [Fact]
    public void ExtrairImagens_LeLegendaDoFigcaption()
    {
        var imagens = _servico.ExtrairImagens(
            "<figure><img src=\"/uploads/imagens/a.jpg\" alt=\"Mapa\"><figcaption>Mapa de 1822</figcaption></figure>" +
            "<p><img src=\"https://exemplo.com/b.png\"></p>");

        Assert.Equal(2, imagens.Count);
        Assert.Equal("Mapa de 1822", imagens[0].Legenda);
        Assert.Equal("Mapa", imagens[0].TextoAlternativo);
        Assert.Null(imagens[1].Legenda);
    }

    [Theory]
    [InlineData("/uploads/imagens/x.png", true)]
    [InlineData("https://exemplo.com/a.jpg", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/uploads/../appsettings.json", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    public void EhUrlSegura_AceitaApenasLocaisOuHttp(string url, bool esperado)
    {
        Assert.Equal(esperado, _servico.EhUrlSegura(url));
    }
}
