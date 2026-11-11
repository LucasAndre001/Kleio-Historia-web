using System.Text;
using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Excecoes;
using KleioHistoria.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace KleioHistoria.Tests;

/// <summary>
/// Os PDFs de teste são gerados na hora com o próprio PdfPig (PdfDocumentBuilder),
/// então os testes não dependem de arquivos externos.
/// </summary>
public class PdfExtracaoServiceTests
{
    private readonly PdfExtracaoService _servico = new(
        Options.Create(new ArmazenamentoOpcoes { CaminhoRaiz = Path.GetTempPath() }),
        NullLogger<PdfExtracaoService>.Instance);

    [Fact]
    public void ExtrairConteudo_DetectaTituloECapitulosPeloTamanhoDaFonte()
    {
        var pdf = CriarPdf(pagina =>
        {
            var y = 780.0;
            Escrever(pagina, "Historia do Brasil Imperial", 24, ref y, 40);
            Escrever(pagina, "Capitulo Um", 16, ref y, 26);
            Escrever(pagina, "Primeira linha do primeiro capitulo com bastante texto corrido.", 11, ref y, 14);
            Escrever(pagina, "Segunda linha do mesmo paragrafo, continuando a ideia anterior.", 11, ref y, 30);
            Escrever(pagina, "Capitulo Dois", 16, ref y, 26);
            Escrever(pagina, "Texto do segundo capitulo, que tambem possui varias palavras.", 11, ref y, 14);
            Escrever(pagina, "Mais uma linha de texto para formar um paragrafo completo.", 11, ref y, 14);
        });

        var resultado = _servico.ExtrairConteudo(pdf);

        Assert.Equal("Historia do Brasil Imperial", resultado.TituloSugerido);
        Assert.Equal(2, resultado.Capitulos.Count);
        Assert.Equal("Capitulo Um", resultado.Capitulos[0].Titulo);
        Assert.Equal("Capitulo Dois", resultado.Capitulos[1].Titulo);
        Assert.Contains("Primeira linha do primeiro capitulo", resultado.Capitulos[0].ConteudoHtml);
        Assert.Contains("<p>", resultado.Capitulos[0].ConteudoHtml);
        Assert.Equal(1, resultado.TotalPaginas);
        Assert.Single(resultado.Paginas);
    }

    [Fact]
    public void ExtrairConteudo_SemTitulos_GeraCapituloUnicoComAviso()
    {
        var pdf = CriarPdf(pagina =>
        {
            var y = 780.0;
            for (var i = 0; i < 6; i++)
                Escrever(pagina, $"Linha {i} de um texto simples sem nenhum titulo destacado.", 11, ref y, 14);
        });

        var resultado = _servico.ExtrairConteudo(pdf);

        Assert.Single(resultado.Capitulos);
        Assert.Equal("Capítulo 1", resultado.Capitulos[0].Titulo);
        Assert.Contains(resultado.Avisos, a => a.Contains("Nenhum título"));
    }

    [Fact]
    public void ExtrairConteudo_SubtituloEmNegritoDoMesmoTamanho_ViraH3()
    {
        var pdf = CriarPdf(pagina =>
        {
            var y = 780.0;
            Escrever(pagina, "Capitulo Um", 16, ref y, 26);
            Escrever(pagina, "Paragrafo inicial do capitulo com texto suficiente para o teste.", 11, ref y, 22);
            Escrever(pagina, "Um subtitulo em negrito", 11, ref y, 18, negrito: true);
            Escrever(pagina, "Texto depois do subtitulo, que continua o mesmo capitulo.", 11, ref y, 30);
            Escrever(pagina, "Capitulo Dois", 16, ref y, 26);
            Escrever(pagina, "Conteudo do segundo capitulo para completar o documento.", 11, ref y, 14);
        });

        var resultado = _servico.ExtrairConteudo(pdf);

        Assert.Equal(2, resultado.Capitulos.Count);
        Assert.Contains("<h3>Um subtitulo em negrito</h3>", resultado.Capitulos[0].ConteudoHtml);
    }

    [Fact]
    public void ExtrairConteudo_TituloEmDuasLinhas_EhReunido()
    {
        var pdf = CriarPdf(pagina =>
        {
            var y = 780.0;
            Escrever(pagina, "A sociedade mineradora nas Minas", 22, ref y, 26);
            Escrever(pagina, "Gerais do seculo XVIII", 22, ref y, 44);
            Escrever(pagina, "Introducao", 15, ref y, 24);
            Escrever(pagina, "Texto da introducao com varias palavras para formar o corpo.", 11, ref y, 30);
            Escrever(pagina, "Desenvolvimento", 15, ref y, 24);
            Escrever(pagina, "Texto do desenvolvimento com varias palavras para o corpo.", 11, ref y, 14);
        });

        var resultado = _servico.ExtrairConteudo(pdf);

        Assert.Equal("A sociedade mineradora nas Minas Gerais do seculo XVIII", resultado.TituloSugerido);
        Assert.Equal(2, resultado.Capitulos.Count);
        Assert.Equal("Introducao", resultado.Capitulos[0].Titulo);
    }

    [Fact]
    public void ExtrairConteudo_RemoveNumeroDePagina_EUneParagrafoEntrePaginas()
    {
        var builder = new PdfDocumentBuilder();
        var fonte = builder.AddStandard14Font(Standard14Font.Helvetica);
        var p1 = builder.AddPage(PageSize.A4);
        p1.AddText("Este paragrafo comeca na primeira pagina e", 11, new PdfPoint(60, 780), fonte);
        p1.AddText("continua sem ponto final no fim da pagina", 11, new PdfPoint(60, 766), fonte);
        p1.AddText("1", 9, new PdfPoint(300, 30), fonte);
        var p2 = builder.AddPage(PageSize.A4);
        p2.AddText("e termina aqui na segunda pagina com ponto final.", 11, new PdfPoint(60, 780), fonte);
        p2.AddText("2", 9, new PdfPoint(300, 30), fonte);

        var resultado = _servico.ExtrairConteudo(builder.Build());
        var html = Assert.Single(resultado.Capitulos).ConteudoHtml;

        Assert.Single(html.Split("<p>", StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("pagina e termina aqui", html);
        Assert.DoesNotContain(">1<", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtrairConteudo_PdfSemTexto_EhRecusadoComoDigitalizado()
    {
        var pdf = CriarPdf(_ => { /* página em branco, como um scan sem OCR */ });

        var erro = Assert.Throws<ArquivoInvalidoExcecao>(() => _servico.ExtrairConteudo(pdf));
        Assert.Contains("OCR", erro.Message);
    }

    [Fact]
    public void ExtrairConteudo_ArquivoCorrompido_EhRecusado()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\nisto nao e um pdf de verdade");
        Assert.Throws<ArquivoInvalidoExcecao>(() => _servico.ExtrairConteudo(bytes));
    }

    [Theory]
    [InlineData(new[] { "revolu-", "cionário francês" }, "revolucionário francês")]
    [InlineData(new[] { "guarda-", "Chuva" }, "guarda- Chuva")]
    [InlineData(new[] { "  primeira  ", "segunda" }, "primeira segunda")]
    public void JuntarLinhas_ReuneHifenizacao(string[] linhas, string esperado)
    {
        Assert.Equal(esperado, PdfExtracaoService.JuntarLinhas(linhas));
    }

    [Theory]
    [InlineData("1. A descoberta do ouro", "A descoberta do ouro")]
    [InlineData("2.1 - Fiscalidade", "Fiscalidade")]
    [InlineData("II - O Império", "O Império")]
    [InlineData("Capítulo 3: Conclusão", "Conclusão")]
    [InlineData("1789: os Estados Gerais", "1789: os Estados Gerais")]
    [InlineData("Introdução", "Introdução")]
    public void RemoverNumeracao_TiraNumeracaoDoPdf(string titulo, string esperado)
    {
        Assert.Equal(esperado, PdfExtracaoService.RemoverNumeracao(titulo));
    }

    [Fact]
    public void LimparTexto_CompoeAcentosETrocaLigaduras()
    {
        // "a" + til combinante (U+0303) deve virar "ã"; "ﬁ" (U+FB01) deve virar "fi".
        Assert.Equal("ação", PdfExtracaoService.LimparTexto("ação"));
        Assert.Equal("ficção", PdfExtracaoService.LimparTexto("ﬁcção"));
    }

    // ---------- auxiliares ----------

    private static byte[] CriarPdf(Action<PdfPageBuilder> desenhar)
    {
        var builder = new PdfDocumentBuilder();
        var pagina = builder.AddPage(PageSize.A4);
        FonteNormal = builder.AddStandard14Font(Standard14Font.Helvetica);
        FonteNegrito = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        desenhar(pagina);
        return builder.Build();
    }

    private static PdfDocumentBuilder.AddedFont FonteNormal = null!;
    private static PdfDocumentBuilder.AddedFont FonteNegrito = null!;

    private static void Escrever(PdfPageBuilder pagina, string texto, double tamanho, ref double y, double avanco, bool negrito = false)
    {
        var fonte = tamanho > 12 || negrito ? FonteNegrito : FonteNormal;
        pagina.AddText(texto, tamanho, new PdfPoint(60, y), fonte);
        y -= avanco;
    }
}
