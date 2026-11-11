using System.Text;
using System.Text.Json;
using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Dtos.Artigos;
using KleioHistoria.API.Dtos.Capitulos;
using KleioHistoria.API.Services;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KleioHistoria.API.Data;

/// <summary>
/// Insere artigos de demonstração a partir de SeedData/artigos-demo.json.
///
/// Os artigos são criados pelo MESMO <see cref="IArtigoService"/> usado pelo painel,
/// passando pela mesma sanitização e geração de slug. Os textos são lorem ipsum e
/// não há imagens: o professor pode inserir capas pelo painel.
/// Se existir a pasta SeedData/imagens, os arquivos dela são copiados para os uploads.
/// </summary>
public static class SeedDemonstrativo
{
    private sealed record ArtigoSeed(
        string Titulo, string Slug, string Resumo, string Categoria, string? Autor,
        string? Capa, bool Destaque, bool Publicado, int DiasDesdePublicacao, List<CapituloSeed> Capitulos);

    private sealed record CapituloSeed(string Titulo, List<string> Paragrafos, FiguraSeed? Figura, string? Citacao);

    private sealed record FiguraSeed(string Arquivo, string Legenda, string Alt);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task SemearAsync(IServiceProvider sp, int usuarioId, ILogger logger, CancellationToken ct)
    {
        var caminhoJson = Path.Combine(AppContext.BaseDirectory, "SeedData", "artigos-demo.json");
        if (!File.Exists(caminhoJson))
        {
            logger.LogWarning("Arquivo de dados demonstrativos não encontrado: {Caminho}", caminhoJson);
            return;
        }

        var artigos = JsonSerializer.Deserialize<List<ArtigoSeed>>(await File.ReadAllTextAsync(caminhoJson, ct), Json) ?? [];
        var opcoes = sp.GetRequiredService<IOptions<ArmazenamentoOpcoes>>().Value;
        var artigoService = sp.GetRequiredService<IArtigoService>();
        var db = sp.GetRequiredService<AppDbContext>();

        CopiarImagens(opcoes, logger);

        foreach (var seed in artigos)
        {
            var dto = new ArtigoSalvarDto
            {
                Titulo = seed.Titulo,
                Slug = seed.Slug,
                Resumo = seed.Resumo,
                Categoria = seed.Categoria,
                Autor = seed.Autor,
                ImagemCapa = seed.Capa is null ? null : UrlImagem(seed.Capa),
                Capitulos = seed.Capitulos.Select(c => new CapituloSalvarDto
                {
                    Titulo = c.Titulo,
                    ConteudoHtml = MontarHtml(c)
                }).ToList()
            };

            var criado = await artigoService.CriarAsync(dto, usuarioId, ct);

            if (seed.Publicado)
            {
                await artigoService.PublicarAsync(criado.Id, ct);
                if (seed.Destaque)
                    await artigoService.DefinirDestaqueAsync(criado.Id, true, ct);

                // Datas no passado, para a ordenação "mais recentes" fazer sentido.
                DateTime? data = DateTime.UtcNow.AddDays(-seed.DiasDesdePublicacao);
                await db.Artigos.Where(a => a.Id == criado.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.DataPublicacao, data)
                    .SetProperty(a => a.DataCriacao, data.Value.AddDays(-2))
                    .SetProperty(a => a.DataAtualizacao, data.Value), ct);
            }
        }

        logger.LogInformation("{Quantidade} artigo(s) demonstrativo(s) inserido(s).", artigos.Count);
    }

    private static string MontarHtml(CapituloSeed capitulo)
    {
        var html = new StringBuilder();
        for (var i = 0; i < capitulo.Paragrafos.Count; i++)
        {
            html.Append("<p>").Append(PdfExtracaoService.CodificarHtml(capitulo.Paragrafos[i])).Append("</p>");

            // A figura entra depois do primeiro parágrafo, e a citação depois do segundo.
            if (i == 0 && capitulo.Figura is { } f)
                html.Append($"<figure><img src=\"{UrlImagem(f.Arquivo)}\" alt=\"{PdfExtracaoService.CodificarHtml(f.Alt)}\">" +
                            $"<figcaption>{PdfExtracaoService.CodificarHtml(f.Legenda)}</figcaption></figure>");
            if (i == 0 && capitulo.Citacao is { } c)
                html.Append($"<blockquote><p>{PdfExtracaoService.CodificarHtml(c)}</p></blockquote>");
        }
        return html.ToString();
    }

    private static string UrlImagem(string arquivo) => $"{ArmazenamentoOpcoes.UrlPublicaImagens}/{arquivo}";

    private static void CopiarImagens(ArmazenamentoOpcoes opcoes, ILogger logger)
    {
        var origem = Path.Combine(AppContext.BaseDirectory, "SeedData", "imagens");
        if (!Directory.Exists(origem))
            return;

        var destino = Path.Combine(Path.GetFullPath(opcoes.CaminhoRaiz), ArmazenamentoOpcoes.PastaImagens);
        Directory.CreateDirectory(destino);

        foreach (var arquivo in Directory.GetFiles(origem))
        {
            var alvo = Path.Combine(destino, Path.GetFileName(arquivo));
            if (!File.Exists(alvo))
                File.Copy(arquivo, alvo);
        }
        logger.LogInformation("Imagens demonstrativas copiadas para {Destino}", destino);
    }
}
