using KleioHistoria.API.Services;

namespace KleioHistoria.Tests;

public class GeradorSlugTests
{
    [Theory]
    [InlineData("Revolução Francesa", "revolucao-francesa")]
    [InlineData("  A Proclamação da República: 1889!  ", "a-proclamacao-da-republica-1889")]
    [InlineData("Ação, coração & ÉTICA", "acao-coracao-etica")]
    [InlineData("Brasil -- Império", "brasil-imperio")]
    public void Gerar_RemoveAcentosPontuacaoEEspacos(string titulo, string esperado)
    {
        Assert.Equal(esperado, GeradorSlug.Gerar(titulo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!!! ???")]
    public void Gerar_TextoSemLetras_RetornaPadrao(string? titulo)
    {
        Assert.Equal("artigo", GeradorSlug.Gerar(titulo));
    }

    [Fact]
    public void Gerar_TituloMuitoLongo_RespeitaTamanhoMaximo()
    {
        var titulo = string.Join(' ', Enumerable.Repeat("historiografia", 40));
        var slug = GeradorSlug.Gerar(titulo);

        Assert.True(slug.Length <= GeradorSlug.TamanhoMaximo);
        Assert.False(slug.EndsWith('-'));
    }
}
