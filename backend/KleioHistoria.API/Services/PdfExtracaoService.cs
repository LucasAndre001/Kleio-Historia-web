using System.Text;
using System.Text.RegularExpressions;
using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Dtos.Pdf;
using KleioHistoria.API.Excecoes;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace KleioHistoria.API.Services;

/// <summary>
/// Importação de PDF com texto selecionável, usando a biblioteca PdfPig (Apache 2.0).
///
/// Como funciona a extração:
/// 1. <b>Palavras</b>: o PDF guarda letras soltas com coordenadas. O
///    <c>NearestNeighbourWordExtractor</c> agrupa letras próximas em palavras,
///    o que evita palavras "grudadas" (problema comum em extratores simples).
/// 2. <b>Blocos</b>: o algoritmo Docstrum agrupa palavras em linhas e blocos
///    (na prática, parágrafos e títulos).
/// 3. <b>Ordem de leitura</b>: segue o fluxo de conteúdo do arquivo (a ordem em que
///    o editor de texto gravou o texto), que respeita colunas e títulos centralizados.
/// 4. <b>Limpeza</b>: remove cabeçalhos/rodapés repetidos e números de página, une
///    títulos quebrados em várias linhas e parágrafos que continuam na página seguinte.
/// 5. <b>Títulos</b>: blocos curtos com fonte maior que a do corpo do texto viram
///    títulos de capítulo (ou subtítulos, se forem de um nível menor).
///
/// O resultado NUNCA é publicado automaticamente: vai para a tela de revisão.
/// </summary>
public partial class PdfExtracaoService(
    IOptions<ArmazenamentoOpcoes> opcoes,
    ILogger<PdfExtracaoService> logger) : IPdfExtracaoService
{
    private static readonly HashSet<string> TiposMimeAceitos =
        new(["application/pdf", "application/x-pdf"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Quantas vezes maior que o corpo do texto a fonte precisa ser para ser considerada título.</summary>
    private const double FatorTitulo = 1.15;
    private const int TamanhoMaximoTitulo = 160;
    private const int MaximoCapitulosDetectados = 60;

    public async Task<PdfImportacaoDto> ImportarAsync(IFormFile arquivo, CancellationToken ct = default)
    {
        var bytes = await ValidarELerAsync(arquivo, ct);
        var extraido = ExtrairConteudo(bytes);

        // Guarda o PDF original no volume (não é servido publicamente).
        var arquivoId = $"{Guid.NewGuid():N}.pdf";
        var pasta = Path.Combine(Path.GetFullPath(opcoes.Value.CaminhoRaiz), ArmazenamentoOpcoes.PastaPdfs);
        Directory.CreateDirectory(pasta);
        await File.WriteAllBytesAsync(Path.Combine(pasta, arquivoId), bytes, ct);

        logger.LogInformation("PDF importado: {Arquivo}, {Paginas} páginas, {Capitulos} capítulos sugeridos",
            arquivoId, extraido.TotalPaginas, extraido.Capitulos.Count);

        return new PdfImportacaoDto(
            arquivoId,
            ValidadorArquivo.NomeSeguroParaExibicao(arquivo.FileName),
            extraido.TotalPaginas,
            extraido.TituloSugerido,
            extraido.ResumoSugerido,
            extraido.Capitulos,
            extraido.Paginas,
            extraido.Avisos);
    }

    // ---------------------------------------------------------------------
    //  Validação do arquivo
    // ---------------------------------------------------------------------

    private async Task<byte[]> ValidarELerAsync(IFormFile? arquivo, CancellationToken ct)
    {
        var limiteMb = opcoes.Value.TamanhoMaximoPdfMb;

        if (arquivo is null || arquivo.Length == 0)
            throw new ArquivoInvalidoExcecao("Nenhum arquivo foi enviado.", StatusCodes.Status400BadRequest);

        if (arquivo.Length > limiteMb * 1024L * 1024L)
            throw new ArquivoInvalidoExcecao($"O PDF excede o limite de {limiteMb} MB.", StatusCodes.Status413PayloadTooLarge);

        if (!string.Equals(Path.GetExtension(arquivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArquivoInvalidoExcecao("O arquivo precisa ter a extensão .pdf.", StatusCodes.Status415UnsupportedMediaType);

        if (!TiposMimeAceitos.Contains(arquivo.ContentType ?? string.Empty))
            throw new ArquivoInvalidoExcecao("O tipo do arquivo (MIME) não é application/pdf.", StatusCodes.Status415UnsupportedMediaType);

        using var memoria = new MemoryStream((int)arquivo.Length);
        await arquivo.CopyToAsync(memoria, ct);
        var bytes = memoria.ToArray();

        // Não confiamos só na extensão: o conteúdo precisa começar como um PDF de verdade.
        if (!ValidadorArquivo.EhPdf(bytes))
            throw new ArquivoInvalidoExcecao("O conteúdo do arquivo não é um PDF válido.", StatusCodes.Status415UnsupportedMediaType);

        return bytes;
    }

    // ---------------------------------------------------------------------
    //  Extração (pública para os testes automatizados)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Bloco de texto já limpo: o tamanho médio da fonte e a posição vertical
    /// (Topo/Base, em pontos, com origem no rodapé da página como no PDF).
    /// </summary>
    internal sealed record BlocoTexto(int Pagina, string Texto, double TamanhoFonte, int Linhas, int Letras, double Topo, double Base, bool Negrito);

    /// <summary>Resultado da análise, antes de virar DTO.</summary>
    public sealed record ConteudoExtraido(
        int TotalPaginas,
        string? TituloSugerido,
        string? ResumoSugerido,
        List<CapituloExtraidoDto> Capitulos,
        List<PaginaExtraidaDto> Paginas,
        List<string> Avisos);

    public ConteudoExtraido ExtrairConteudo(byte[] bytes)
    {
        var avisos = new List<string>();
        var paginas = new List<PaginaExtraidaDto>();
        var blocos = new List<BlocoTexto>();
        var paginasSemTexto = new List<int>();
        string? tituloMetadados;
        int totalPaginas;

        try
        {
            using var documento = PdfDocument.Open(bytes);
            totalPaginas = documento.NumberOfPages;
            tituloMetadados = documento.Information?.Title;

            foreach (var pagina in documento.GetPages())
            {
                var letrasVisiveis = pagina.Letters.Count(l => !string.IsNullOrWhiteSpace(l.Value));
                if (letrasVisiveis < 10)
                {
                    paginasSemTexto.Add(pagina.Number);
                    paginas.Add(new PaginaExtraidaDto(pagina.Number, string.Empty));
                    continue;
                }

                var blocosPagina = ExtrairBlocosDaPagina(pagina);
                blocos.AddRange(blocosPagina);
                paginas.Add(new PaginaExtraidaDto(pagina.Number, string.Join("\n\n", blocosPagina.Select(b => b.Texto))));
            }
        }
        catch (KleioExcecao)
        {
            throw;
        }
        catch (Exception ex)
        {
            // PDF corrompido, criptografado ou fora do padrão.
            logger.LogWarning(ex, "Falha ao ler PDF");
            throw new ArquivoInvalidoExcecao("Não foi possível ler o PDF. O arquivo pode estar corrompido ou protegido por senha.");
        }

        if (blocos.Count == 0)
        {
            throw new ArquivoInvalidoExcecao(
                "Este PDF não possui texto selecionável — provavelmente foi digitalizado como imagem. " +
                "O reconhecimento de texto (OCR) não é suportado nesta versão.");
        }

        if (paginasSemTexto.Count > 0)
        {
            avisos.Add($"{paginasSemTexto.Count} página(s) sem texto selecionável foram ignoradas " +
                       $"(páginas {string.Join(", ", paginasSemTexto.Take(10))}{(paginasSemTexto.Count > 10 ? "..." : "")}). " +
                       "Podem ser imagens digitalizadas.");
        }

        blocos = RemoverCabecalhosERodapes(blocos, totalPaginas, avisos);
        var tamanhoCorpo = CalcularTamanhoDoCorpo(blocos);
        blocos = UnirTitulosDeVariasLinhas(blocos, tamanhoCorpo);
        blocos = UnirParagrafosEntrePaginas(blocos, tamanhoCorpo);
        return MontarCapitulos(blocos, tamanhoCorpo, totalPaginas, tituloMetadados, paginas, avisos);
    }

    /// <summary>
    /// Agrupa letras → palavras → blocos e devolve os blocos na ordem de leitura.
    /// </summary>
    private static List<BlocoTexto> ExtrairBlocosDaPagina(Page pagina)
    {
        // Ordem de leitura = ordem em que o texto foi escrito no arquivo (fluxo de conteúdo).
        // Editores como Word, LibreOffice e LaTeX gravam o texto na ordem lógica, o que
        // se mostrou mais confiável do que reordenar os blocos pela geometria da página.
        var posicaoNoFluxo = new Dictionary<Letter, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < pagina.Letters.Count; i++)
            posicaoNoFluxo[pagina.Letters[i]] = i;

        var palavras = NearestNeighbourWordExtractor.Instance.GetWords(pagina.Letters).ToList();
        var blocosDocstrum = DocstrumBoundingBoxes.Instance.GetBlocks(palavras)
            .OrderBy(b => b.TextLines.SelectMany(l => l.Words).SelectMany(w => w.Letters)
                .Select(l => posicaoNoFluxo.GetValueOrDefault(l, int.MaxValue)).DefaultIfEmpty(int.MaxValue).Min());

        var resultado = new List<BlocoTexto>();
        foreach (var bloco in blocosDocstrum)
        {
            // Um bloco pode misturar um subtítulo e o parágrafo seguinte (o Docstrum os
            // agrupa quando estão próximos). Separamos linhas consecutivas por tamanho de fonte.
            // Também separamos quando muda o negrito (subtítulo em negrito do mesmo tamanho).
            var grupo = new List<(TextLine Linha, double Tamanho, int Letras, bool Negrito)>();
            foreach (var linha in bloco.TextLines)
            {
                var letras = linha.Words.SelectMany(w => w.Letters).Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
                if (letras.Count == 0) continue;
                var tamanho = letras.Average(l => l.PointSize);
                var negrito = letras.Count(EhNegrito) >= letras.Count * 0.9;

                if (grupo.Count > 0 && (Math.Abs(Math.Round(grupo[^1].Tamanho * 2) - Math.Round(tamanho * 2)) >= 1
                                        || grupo[^1].Negrito != negrito))
                {
                    AdicionarGrupo(resultado, pagina.Number, grupo);
                    grupo.Clear();
                }
                grupo.Add((linha, tamanho, letras.Count, negrito));
            }
            AdicionarGrupo(resultado, pagina.Number, grupo);
        }
        return resultado;
    }

    private static void AdicionarGrupo(List<BlocoTexto> destino, int pagina, List<(TextLine Linha, double Tamanho, int Letras, bool Negrito)> grupo)
    {
        if (grupo.Count == 0) return;
        var texto = JuntarLinhas(grupo.Select(g => g.Linha.Text));
        if (texto.Length == 0) return;

        var totalLetras = grupo.Sum(g => g.Letras);
        var tamanhoMedio = grupo.Sum(g => g.Tamanho * g.Letras) / totalLetras;
        destino.Add(new BlocoTexto(pagina, texto, tamanhoMedio, grupo.Count, totalLetras,
            grupo.Max(g => g.Linha.BoundingBox.Top), grupo.Min(g => g.Linha.BoundingBox.Bottom),
            grupo.All(g => g.Negrito)));
    }

    private static bool EhNegrito(Letter letra)
    {
        if (letra.Font?.IsBold == true) return true;
        var nome = letra.FontName ?? string.Empty;
        return nome.Contains("bold", StringComparison.OrdinalIgnoreCase)
               || nome.Contains("black", StringComparison.OrdinalIgnoreCase)
               || nome.Contains("heavy", StringComparison.OrdinalIgnoreCase)
               || nome.Contains("semibold", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Um bloco é candidato a título se for curto e:
    /// (a) tiver fonte claramente maior que o corpo do texto; ou
    /// (b) estiver todo em negrito, com fonte igual ou pouco maior, sem terminar em ponto
    ///     (subtítulos comuns em trabalhos acadêmicos).
    /// </summary>
    private static bool PareceTitulo(BlocoTexto b, double tamanhoCorpo)
    {
        if (b.Texto.Length > TamanhoMaximoTitulo || b.Linhas > 3)
            return false;
        if (b.TamanhoFonte >= tamanhoCorpo * FatorTitulo)
            return true;
        return b.Negrito && b.Linhas <= 2
               && b.TamanhoFonte >= tamanhoCorpo - 0.5
               && !b.Texto.TrimEnd().EndsWith('.');
    }

    /// <summary>
    /// Títulos longos ocupam duas ou três linhas e podem chegar como blocos separados
    /// ("A sociedade mineradora do século" + "XVIII"). Reunimos blocos de título
    /// consecutivos, com a mesma fonte e verticalmente próximos.
    /// </summary>
    private static List<BlocoTexto> UnirTitulosDeVariasLinhas(List<BlocoTexto> blocos, double tamanhoCorpo)
    {
        var resultado = new List<BlocoTexto>();
        foreach (var bloco in blocos)
        {
            if (resultado.Count > 0)
            {
                var anterior = resultado[^1];
                var distancia = anterior.Base - bloco.Topo;
                if (anterior.Pagina == bloco.Pagina
                    && PareceTitulo(anterior, tamanhoCorpo) && PareceTitulo(bloco, tamanhoCorpo)
                    && Math.Abs(anterior.TamanhoFonte - bloco.TamanhoFonte) < 0.6
                    && distancia >= -1 && distancia < bloco.TamanhoFonte * 1.2
                    && anterior.Texto.Length + bloco.Texto.Length <= TamanhoMaximoTitulo)
                {
                    resultado[^1] = anterior with
                    {
                        Texto = $"{anterior.Texto} {bloco.Texto}",
                        Linhas = anterior.Linhas + bloco.Linhas,
                        Letras = anterior.Letras + bloco.Letras,
                        Base = bloco.Base
                    };
                    continue;
                }
            }
            resultado.Add(bloco);
        }
        return resultado;
    }

    /// <summary>
    /// Um parágrafo que começa numa página (ou coluna) e termina na seguinte chega como
    /// dois blocos. Se o primeiro não termina com pontuação final e o segundo começa em
    /// minúscula, eles são o mesmo parágrafo.
    /// </summary>
    private static List<BlocoTexto> UnirParagrafosEntrePaginas(List<BlocoTexto> blocos, double tamanhoCorpo)
    {
        var resultado = new List<BlocoTexto>();
        foreach (var bloco in blocos)
        {
            if (resultado.Count > 0)
            {
                var anterior = resultado[^1];
                // Próxima página, ou próxima coluna (o bloco "volta" para o alto da mesma página).
                var continuaAdiante = bloco.Pagina == anterior.Pagina + 1
                                      || (bloco.Pagina == anterior.Pagina && bloco.Topo > anterior.Topo + 5);
                if (continuaAdiante
                    && !PareceTitulo(anterior, tamanhoCorpo) && !PareceTitulo(bloco, tamanhoCorpo)
                    && !FimDeFrase().IsMatch(anterior.Texto)
                    && char.IsLower(bloco.Texto[0]))
                {
                    resultado[^1] = anterior with
                    {
                        Texto = JuntarLinhas([anterior.Texto, bloco.Texto]),
                        Linhas = anterior.Linhas + bloco.Linhas,
                        Letras = anterior.Letras + bloco.Letras
                    };
                    continue;
                }
            }
            resultado.Add(bloco);
        }
        return resultado;
    }

    // ---------------------------------------------------------------------
    //  Organização em capítulos
    // ---------------------------------------------------------------------

    private static ConteudoExtraido MontarCapitulos(
        List<BlocoTexto> blocos, double tamanhoCorpo, int totalPaginas, string? tituloMetadados,
        List<PaginaExtraidaDto> paginas, List<string> avisos)
    {
        var titulos = blocos.Where(b => PareceTitulo(b, tamanhoCorpo)).ToList();

        // Título do documento: o maior título da primeira página.
        var blocoTituloDocumento = titulos
            .Where(b => b.Pagina == blocos[0].Pagina)
            .OrderByDescending(b => b.TamanhoFonte)
            .FirstOrDefault();

        // Só é título do documento se for claramente MAIOR que todos os outros títulos;
        // caso contrário, é apenas o primeiro capítulo.
        if (blocoTituloDocumento is not null &&
            titulos.Any(t => t != blocoTituloDocumento && t.TamanhoFonte >= blocoTituloDocumento.TamanhoFonte - 0.5))
            blocoTituloDocumento = null;

        var tituloSugerido = TituloDosMetadadosEhUtil(tituloMetadados) ? tituloMetadados!.Trim() : blocoTituloDocumento?.Texto;
        titulos.Remove(blocoTituloDocumento!);

        // Nível de capítulo = o maior tamanho de fonte que se repete entre os títulos.
        // Títulos menores viram subtítulos (<h3>) dentro do capítulo.
        double? tamanhoCapitulo = null;
        if (titulos.Count > 0)
        {
            var grupos = titulos.GroupBy(t => Math.Round(t.TamanhoFonte)).OrderByDescending(g => g.Key).ToList();
            tamanhoCapitulo = (grupos.FirstOrDefault(g => g.Count() >= 2) ?? grupos[0]).Key - 0.5;
        }

        var titulosDeCapitulo = titulos.Where(t => t.TamanhoFonte >= tamanhoCapitulo).ToHashSet();
        if (titulosDeCapitulo.Count > MaximoCapitulosDetectados)
        {
            avisos.Add("Foram detectados títulos demais para serem capítulos; eles foram mantidos como subtítulos. " +
                       "Use \"Dividir capítulo aqui\" no editor para organizar.");
            titulosDeCapitulo.Clear();
        }
        var subtitulos = titulos.Where(t => !titulosDeCapitulo.Contains(t)).ToHashSet();

        var capitulos = new List<(string Titulo, StringBuilder Html)>();
        var resumo = new StringBuilder();
        var lendoResumo = false;
        var tituloPadrao = titulosDeCapitulo.Count > 0 ? "Introdução" : "Capítulo 1";

        StringBuilder CapituloAtual()
        {
            if (capitulos.Count == 0)
                capitulos.Add((tituloPadrao, new StringBuilder()));
            return capitulos[^1].Html;
        }

        foreach (var bloco in blocos)
        {
            if (bloco == blocoTituloDocumento)
                continue;

            if (titulosDeCapitulo.Contains(bloco) || subtitulos.Contains(bloco))
            {
                // Seção "Resumo"/"Abstract": vira o resumo sugerido, não um capítulo.
                if (EhTituloDeResumo(bloco.Texto) && resumo.Length == 0)
                {
                    lendoResumo = true;
                    continue;
                }
                lendoResumo = false;

                if (titulosDeCapitulo.Contains(bloco))
                    capitulos.Add((Limitar(RemoverNumeracao(bloco.Texto), 200), new StringBuilder()));
                else
                    CapituloAtual().Append("<h3>").Append(CodificarHtml(bloco.Texto)).Append("</h3>\n");
                continue;
            }

            if (lendoResumo)
            {
                if (resumo.Length > 0) resumo.Append(' ');
                resumo.Append(bloco.Texto);
                continue;
            }

            CapituloAtual().Append("<p>").Append(CodificarHtml(bloco.Texto)).Append("</p>\n");
        }

        var capitulosFinais = capitulos
            .Where(c => c.Html.Length > 0)
            .Select(c => new CapituloExtraidoDto(c.Titulo, c.Html.ToString().Trim()))
            .ToList();

        if (capitulos.Count > capitulosFinais.Count)
            avisos.Add($"{capitulos.Count - capitulosFinais.Count} título(s) sem texto abaixo foram descartados.");

        if (titulosDeCapitulo.Count == 0)
            avisos.Add("Nenhum título de capítulo foi identificado automaticamente. Todo o texto está em um único capítulo: " +
                       "use \"Dividir capítulo aqui\" no editor para separá-lo.");

        avisos.Add("Revise o texto extraído antes de publicar: quebras de linha, hifenização e tabelas podem exigir ajustes.");

        var resumoTexto = resumo.Length == 0 ? null : Limitar(resumo.ToString(), 600);
        return new ConteudoExtraido(totalPaginas, tituloSugerido, resumoTexto, capitulosFinais, paginas, avisos);
    }

    /// <summary>
    /// Tamanho de fonte mais usado (ponderado pelo número de letras) = corpo do texto.
    /// </summary>
    private static double CalcularTamanhoDoCorpo(List<BlocoTexto> blocos) =>
        blocos
            .GroupBy(b => Math.Round(b.TamanhoFonte * 2) / 2) // arredonda para 0,5 pt
            .OrderByDescending(g => g.Sum(b => b.Letras))
            .First().Key;

    /// <summary>
    /// Remove números de página e textos que se repetem no topo/rodapé da maioria das páginas.
    /// </summary>
    private static List<BlocoTexto> RemoverCabecalhosERodapes(List<BlocoTexto> blocos, int totalPaginas, List<string> avisos)
    {
        var repetidos = new HashSet<string>();
        if (totalPaginas >= 3)
        {
            var minimo = Math.Max(3, totalPaginas / 2);
            repetidos = blocos
                .Where(b => b.Texto.Length <= 80)
                .GroupBy(b => NumeroNoTexto().Replace(b.Texto, "#"))
                .Where(g => g.Select(b => b.Pagina).Distinct().Count() >= minimo)
                .Select(g => g.Key)
                .ToHashSet();
        }

        var resultado = blocos
            .Where(b => !NumeroDePagina().IsMatch(b.Texto))
            .Where(b => !repetidos.Contains(NumeroNoTexto().Replace(b.Texto, "#")))
            .ToList();

        if (resultado.Count < blocos.Count)
            avisos.Add($"{blocos.Count - resultado.Count} bloco(s) de cabeçalho, rodapé ou numeração de página foram removidos.");

        return resultado.Count > 0 ? resultado : blocos;
    }

    // ---------------------------------------------------------------------
    //  Limpeza de texto
    // ---------------------------------------------------------------------

    /// <summary>
    /// Une as linhas de um bloco em um parágrafo. Palavras hifenizadas na quebra
    /// de linha ("revolu-" + "cionário") são reunidas.
    /// </summary>
    internal static string JuntarLinhas(IEnumerable<string> linhas)
    {
        var sb = new StringBuilder();
        foreach (var bruta in linhas)
        {
            var linha = LimparTexto(bruta);
            if (linha.Length == 0)
                continue;

            if (sb.Length == 0)
            {
                sb.Append(linha);
            }
            else if (sb.Length > 1 && sb[^1] == '-' && char.IsLetter(sb[^2]) && char.IsLower(linha[0]))
            {
                sb.Length--; // remove o hífen de quebra de linha
                sb.Append(linha);
            }
            else
            {
                sb.Append(' ').Append(linha);
            }
        }
        return EspacosRepetidos().Replace(sb.ToString(), " ").Trim();
    }

    /// <summary>
    /// Normaliza o texto: compõe acentos (a + ~ → ã), troca ligaduras tipográficas
    /// (ﬁ → fi) e remove hífens invisíveis.
    /// </summary>
    internal static string LimparTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        var sb = new StringBuilder(texto.Normalize(NormalizationForm.FormC));
        sb.Replace("­", string.Empty) // hífen suave
          .Replace("ﬀ", "ff").Replace("ﬁ", "fi").Replace("ﬂ", "fl")
          .Replace("ﬃ", "ffi").Replace("ﬄ", "ffl").Replace("ﬅ", "st").Replace("ﬆ", "st")
          .Replace(' ', ' ');
        return sb.ToString().Trim();
    }

    /// <summary>
    /// O site já numera os capítulos ("1.", "2."...). Remove a numeração que vem do PDF
    /// ("1. Introdução", "II - Contexto", "Capítulo 3: Fim") para não ficar "1. 1. Introdução".
    /// Anos como "1789: ..." são preservados (só 1 ou 2 dígitos são considerados numeração).
    /// </summary>
    internal static string RemoverNumeracao(string titulo)
    {
        var limpo = NumeracaoDeTitulo().Replace(titulo, string.Empty).Trim();
        return limpo.Length >= 2 ? limpo : titulo.Trim();
    }

    /// <summary>Codifica apenas os caracteres especiais do HTML, preservando acentos legíveis.</summary>
    internal static string CodificarHtml(string texto) =>
        texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static bool EhTituloDeResumo(string texto)
    {
        var t = texto.Trim().TrimEnd(':', '.').ToLowerInvariant();
        return t is "resumo" or "abstract" or "sumário executivo";
    }

    /// <summary>
    /// Muitos programas gravam metadados inúteis ("Untitled", "(anonymous)",
    /// "Microsoft Word - arquivo.docx"); nesses casos usamos o maior título da 1ª página.
    /// </summary>
    private static bool TituloDosMetadadosEhUtil(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo)) return false;
        var t = titulo.Trim();
        string[] genericos = ["untitled", "anonymous", "sem título", "documento", "microsoft word", "apresentação"];
        return t.Length >= 4
               && !genericos.Any(g => t.Contains(g, StringComparison.OrdinalIgnoreCase))
               && !ExtensaoDeArquivo().IsMatch(t);
    }

    private static string Limitar(string texto, int max) => texto.Length <= max ? texto : texto[..max].TrimEnd();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();

    [GeneratedRegex(@"^\s*((p(á|a)g(ina)?|p)\.?\s*)?\d{1,4}(\s*(de|/|of)\s*\d{1,4})?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex NumeroDePagina();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumeroNoTexto();

    [GeneratedRegex(@"^\s*(?:(?i:cap[ií]tulo)\s+)?(?:\d{1,2}(?:\.\d{1,2})*|[IVXLC]{1,6})\s*(?:[.)]|[-–—:])\s+(?=\S)")]
    private static partial Regex NumeracaoDeTitulo();

    [GeneratedRegex(@"\.(docx?|pdf|odt|rtf)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExtensaoDeArquivo();

    [GeneratedRegex(@"[.!?:;…""”»)\]]\s*$")]
    private static partial Regex FimDeFrase();
}
