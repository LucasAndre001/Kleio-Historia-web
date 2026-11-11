using KleioHistoria.API.Configuracoes;
using KleioHistoria.API.Data;
using KleioHistoria.API.Dtos.Artigos;
using KleioHistoria.API.Dtos.Capitulos;
using KleioHistoria.API.Excecoes;
using KleioHistoria.API.Models.Entities;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace KleioHistoria.API.Services;

/// <summary>
/// Regras de negócio dos artigos. Os Controllers apenas recebem a requisição
/// e delegam para cá; toda validação de negócio, sanitização e acesso ao banco
/// acontecem nesta camada.
/// </summary>
public class ArtigoService(
    AppDbContext db,
    IConteudoHtmlService conteudoHtml,
    IOptions<ArmazenamentoOpcoes> armazenamento,
    ILogger<ArtigoService> logger) : IArtigoService
{
    private const int PalavrasPorMinuto = 200;

    // =====================================================================
    //  ÁREA PÚBLICA
    // =====================================================================

    public async Task<IReadOnlyList<ArtigoResumoDto>> ListarPublicadosAsync(FiltroArtigosDto filtro, CancellationToken ct = default)
    {
        // AsNoTracking: consulta somente leitura, mais rápida (o EF não monitora alterações).
        var consulta = db.Artigos.AsNoTracking()
            .Where(a => a.Status == StatusArtigo.Publicado);

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            var categoria = filtro.Categoria.Trim();
            consulta = consulta.Where(a => a.Categoria == categoria);
        }

        if (filtro.Destaque == true)
            consulta = consulta.Where(a => a.Destaque);

        // Pesquisa por palavras-chave: cada termo precisa aparecer em algum campo
        // (título, resumo, categoria, título ou texto de algum capítulo).
        // unaccent + ILIKE = busca sem diferenciar maiúsculas nem acentos.
        foreach (var termo in QuebrarEmTermos(filtro.Busca))
        {
            var padrao = $"%{EscaparLike(termo)}%";
            consulta = consulta.Where(a =>
                EF.Functions.ILike(EF.Functions.Unaccent(a.Titulo), EF.Functions.Unaccent(padrao), "\\") ||
                EF.Functions.ILike(EF.Functions.Unaccent(a.Resumo), EF.Functions.Unaccent(padrao), "\\") ||
                EF.Functions.ILike(EF.Functions.Unaccent(a.Categoria), EF.Functions.Unaccent(padrao), "\\") ||
                a.Capitulos.Any(c =>
                    EF.Functions.ILike(EF.Functions.Unaccent(c.Titulo), EF.Functions.Unaccent(padrao), "\\") ||
                    EF.Functions.ILike(EF.Functions.Unaccent(c.ConteudoTexto), EF.Functions.Unaccent(padrao), "\\")));
        }

        consulta = (filtro.Ordem ?? "recentes").ToLowerInvariant() switch
        {
            "antigos" => consulta.OrderBy(a => a.DataPublicacao).ThenBy(a => a.Id),
            "titulo" => consulta.OrderBy(a => a.Titulo),
            _ => consulta.OrderByDescending(a => a.DataPublicacao).ThenByDescending(a => a.Id)
        };

        // Projeção direta para o DTO: o SQL busca só as colunas necessárias.
        return await consulta
            .Select(a => new ArtigoResumoDto(
                a.Id, a.Titulo, a.Slug, a.Resumo, a.Categoria, a.ImagemCapa, a.ImagemBanner, a.Autor,
                a.DataPublicacao, a.TempoLeituraMinutos, a.Destaque))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListarCategoriasAsync(CancellationToken ct = default)
    {
        return await db.Artigos.AsNoTracking()
            .Where(a => a.Status == StatusArtigo.Publicado)
            .Select(a => a.Categoria)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<ArtigoDetalheDto> ObterPublicadoPorSlugAsync(string slug, CancellationToken ct = default)
    {
        var artigo = await ConsultaComCapitulos()
            .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == StatusArtigo.Publicado, ct);

        // Rascunhos respondem 404, exatamente como um slug inexistente:
        // o visitante não consegue nem descobrir que o rascunho existe.
        if (artigo is null)
            throw new RecursoNaoEncontradoExcecao("Artigo não encontrado.");

        return new ArtigoDetalheDto(
            artigo.Id, artigo.Titulo, artigo.Slug, artigo.Resumo, artigo.Categoria,
            artigo.ImagemCapa, artigo.Autor, artigo.DataPublicacao, artigo.TempoLeituraMinutos,
            MapearCapitulos(artigo));
    }

    // =====================================================================
    //  ÁREA ADMINISTRATIVA
    // =====================================================================

    public async Task<IReadOnlyList<ArtigoAdminResumoDto>> ListarTodosAsync(CancellationToken ct = default)
    {
        return await db.Artigos.AsNoTracking()
            .OrderByDescending(a => a.DataAtualizacao)
            .Select(a => new ArtigoAdminResumoDto(
                a.Id, a.Titulo, a.Slug, a.Categoria, a.ImagemCapa, a.Status, a.Destaque,
                a.Capitulos.Count, a.DataCriacao, a.DataAtualizacao, a.DataPublicacao))
            .ToListAsync(ct);
    }

    public async Task<ArtigoAdminDetalheDto> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var artigo = await ConsultaComCapitulos().FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");
        return MapearAdmin(artigo);
    }

    public async Task<ArtigoAdminDetalheDto> CriarAsync(ArtigoSalvarDto dto, int usuarioId, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var artigo = new Artigo
        {
            UsuarioId = usuarioId,
            Status = StatusArtigo.Rascunho, // todo artigo nasce como rascunho
            DataCriacao = agora,
            DataAtualizacao = agora
        };

        AplicarDados(artigo, dto);
        artigo.Slug = await GerarSlugUnicoAsync(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Titulo : dto.Slug, idIgnorado: null, ct);

        db.Artigos.Add(artigo);
        await SalvarComTratamentoDeConflitoAsync(ct);

        logger.LogInformation("Artigo {Id} criado ({Slug})", artigo.Id, artigo.Slug);
        return await ObterPorIdAsync(artigo.Id, ct);
    }

    public async Task<ArtigoAdminDetalheDto> AtualizarAsync(int id, ArtigoSalvarDto dto, CancellationToken ct = default)
    {
        var artigo = await db.Artigos
            .Include(a => a.Capitulos)
            .ThenInclude(c => c.Imagens)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");

        // Regra do slug:
        //  - Se o professor informou um slug, ele é usado (garantindo unicidade).
        //  - Senão, enquanto o artigo nunca foi publicado, o slug acompanha o título.
        //  - Depois da primeira publicação o slug fica fixo, para não quebrar links já compartilhados.
        if (!string.IsNullOrWhiteSpace(dto.Slug) && GeradorSlug.Gerar(dto.Slug) != artigo.Slug)
            artigo.Slug = await GerarSlugUnicoAsync(dto.Slug, artigo.Id, ct);
        else if (string.IsNullOrWhiteSpace(dto.Slug) && artigo.DataPublicacao is null)
            artigo.Slug = await GerarSlugUnicoAsync(dto.Titulo, artigo.Id, ct);

        // Os capítulos são substituídos pela nova lista enviada pelo editor.
        // Removê-los e recriá-los é a forma mais simples de refletir
        // inclusões, exclusões e reordenações de uma só vez.
        db.Capitulos.RemoveRange(artigo.Capitulos);
        artigo.Capitulos.Clear();

        AplicarDados(artigo, dto);
        artigo.DataAtualizacao = DateTime.UtcNow;

        // Um artigo publicado precisa continuar válido para publicação.
        if (artigo.Status == StatusArtigo.Publicado)
            ValidarParaPublicacao(artigo);

        await SalvarComTratamentoDeConflitoAsync(ct);
        return await ObterPorIdAsync(artigo.Id, ct);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var artigo = await db.Artigos.FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");

        // Capítulos e imagens são apagados em cascata pelo banco.
        db.Artigos.Remove(artigo);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Artigo {Id} excluído", id);
    }

    public async Task<ArtigoAdminDetalheDto> PublicarAsync(int id, CancellationToken ct = default)
    {
        var artigo = await db.Artigos.Include(a => a.Capitulos)
                         .FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");

        ValidarParaPublicacao(artigo);

        artigo.Status = StatusArtigo.Publicado;
        // Mantém a data da primeira publicação caso o artigo seja republicado.
        artigo.DataPublicacao ??= DateTime.UtcNow;
        artigo.DataAtualizacao = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Artigo {Id} publicado", id);
        return await ObterPorIdAsync(id, ct);
    }

    public async Task<ArtigoAdminDetalheDto> DespublicarAsync(int id, CancellationToken ct = default)
    {
        var artigo = await db.Artigos.FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");

        artigo.Status = StatusArtigo.Rascunho;
        artigo.Destaque = false; // rascunho não pode aparecer no carrossel
        artigo.DataAtualizacao = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Artigo {Id} despublicado", id);
        return await ObterPorIdAsync(id, ct);
    }

    public async Task DefinirDestaqueAsync(int id, bool destaque, CancellationToken ct = default)
    {
        var artigo = await db.Artigos.FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoExcecao($"Artigo {id} não encontrado.");

        if (destaque && artigo.Status != StatusArtigo.Publicado)
            throw new RegraNegocioExcecao("Somente artigos publicados podem ficar em destaque.");

        artigo.Destaque = destaque;
        await db.SaveChangesAsync(ct);
    }

    // =====================================================================
    //  AUXILIARES
    // =====================================================================

    /// <summary>Copia os dados do DTO para a entidade, sanitizando o HTML de cada capítulo.</summary>
    private void AplicarDados(Artigo artigo, ArtigoSalvarDto dto)
    {
        artigo.Titulo = dto.Titulo.Trim();
        artigo.Resumo = dto.Resumo?.Trim() ?? string.Empty;
        artigo.Categoria = dto.Categoria.Trim();
        artigo.Autor = string.IsNullOrWhiteSpace(dto.Autor) ? null : dto.Autor.Trim();

        artigo.ImagemCapa = ValidarUrlImagem(dto.ImagemCapa, "A imagem de capa");
        artigo.ImagemBanner = ValidarUrlImagem(dto.ImagemBanner, "A imagem do banner");

        if (!string.IsNullOrWhiteSpace(dto.ArquivoPdfOrigem))
            artigo.ArquivoPdfOrigem = ValidarArquivoPdfOrigem(dto.ArquivoPdfOrigem);

        // Destaque só faz sentido para artigos publicados.
        artigo.Destaque = dto.Destaque && artigo.Status == StatusArtigo.Publicado;

        var totalPalavras = 0;
        var ordem = 1;
        foreach (var capituloDto in dto.Capitulos)
        {
            var html = conteudoHtml.Sanitizar(capituloDto.ConteudoHtml);
            var texto = conteudoHtml.ExtrairTexto(html);
            totalPalavras += ContarPalavras(texto);

            var capitulo = new Capitulo
            {
                Ordem = ordem++,
                Titulo = capituloDto.Titulo.Trim(),
                ConteudoHtml = html,
                ConteudoTexto = texto
            };

            // A tabela Imagens espelha as imagens presentes no HTML do capítulo.
            var posicao = 1;
            foreach (var img in conteudoHtml.ExtrairImagens(html))
            {
                capitulo.Imagens.Add(new Imagem
                {
                    Url = img.Url,
                    Legenda = img.Legenda,
                    TextoAlternativo = img.TextoAlternativo,
                    Ordem = posicao++
                });
            }

            artigo.Capitulos.Add(capitulo);
        }

        artigo.TempoLeituraMinutos = totalPalavras == 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling(totalPalavras / (double)PalavrasPorMinuto));
    }

    private static void ValidarParaPublicacao(Artigo artigo)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(artigo.Resumo))
            erros.Add("informe um resumo");
        if (artigo.Capitulos.Count == 0)
            erros.Add("adicione pelo menos um capítulo");
        else if (artigo.Capitulos.All(c => string.IsNullOrWhiteSpace(c.ConteudoTexto) && c.Imagens.Count == 0))
            erros.Add("escreva o conteúdo de pelo menos um capítulo");

        if (erros.Count > 0)
            throw new RegraNegocioExcecao($"Não é possível publicar: {string.Join("; ", erros)}.");
    }

    /// <summary>Garante que o slug não está em uso, acrescentando -2, -3... se necessário.</summary>
    private async Task<string> GerarSlugUnicoAsync(string textoBase, int? idIgnorado, CancellationToken ct)
    {
        var baseSlug = GeradorSlug.Gerar(textoBase);
        if (baseSlug.Length > GeradorSlug.TamanhoMaximo - 5)
            baseSlug = baseSlug[..(GeradorSlug.TamanhoMaximo - 5)].Trim('-');

        var existentes = await db.Artigos.AsNoTracking()
            .Where(a => (a.Slug == baseSlug || a.Slug.StartsWith(baseSlug + "-"))
                        && (idIgnorado == null || a.Id != idIgnorado))
            .Select(a => a.Slug)
            .ToListAsync(ct);

        if (!existentes.Contains(baseSlug))
            return baseSlug;

        var sufixo = 2;
        while (existentes.Contains($"{baseSlug}-{sufixo}"))
            sufixo++;
        return $"{baseSlug}-{sufixo}";
    }

    /// <summary>
    /// O índice único do banco é a última linha de defesa: se dois salvamentos
    /// simultâneos gerarem o mesmo slug, devolvemos 409 em vez de erro 500.
    /// </summary>
    private async Task SalvarComTratamentoDeConflitoAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflitoExcecao("Já existe um artigo com este slug. Escolha outro título ou slug.");
        }
    }

    private string ValidarArquivoPdfOrigem(string nome)
    {
        // Só aceitamos o identificador gerado pela própria API (32 hex + .pdf),
        // o que impede caminhos como "../../etc/passwd".
        var limpo = Path.GetFileName(nome.Trim());
        var ehValido = limpo.Length == 36
                       && limpo.EndsWith(".pdf", StringComparison.Ordinal)
                       && limpo[..32].All(Uri.IsHexDigit);
        if (!ehValido)
            throw new RegraNegocioExcecao("Identificador de PDF inválido.");

        var caminho = Path.Combine(ResolverPastaPdfs(), limpo);
        if (!File.Exists(caminho))
            throw new RegraNegocioExcecao("O PDF importado não foi encontrado. Importe o arquivo novamente.");
        return limpo;
    }

    private string ResolverPastaPdfs() =>
        Path.Combine(Path.GetFullPath(armazenamento.Value.CaminhoRaiz), ArmazenamentoOpcoes.PastaPdfs);

    private IQueryable<Artigo> ConsultaComCapitulos() =>
        db.Artigos.AsNoTracking()
            .Include(a => a.Capitulos.OrderBy(c => c.Ordem))
            .ThenInclude(c => c.Imagens.OrderBy(i => i.Ordem))
            .AsSplitQuery(); // evita "explosão cartesiana" ao incluir duas coleções

    private static List<CapituloDto> MapearCapitulos(Artigo artigo) =>
        artigo.Capitulos
            .OrderBy(c => c.Ordem)
            .Select(c => new CapituloDto(
                c.Id, c.Ordem, c.Titulo, c.ConteudoHtml,
                c.Imagens.OrderBy(i => i.Ordem)
                    .Select(i => new ImagemCapituloDto(i.Id, i.Url, i.Legenda, i.TextoAlternativo))
                    .ToList()))
            .ToList();

    /// <summary>
    /// Aceita imagem enviada pelo painel (/uploads/imagens/...) ou link http(s) da internet.
    /// Qualquer outro esquema (javascript:, data:...) é recusado.
    /// </summary>
    private string? ValidarUrlImagem(string? url, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var limpa = url.Trim();
        if (!conteudoHtml.EhUrlSegura(limpa))
            throw new RegraNegocioExcecao($"{nomeCampo} deve ser um upload local ou um endereço http(s).");
        return limpa;
    }

    private static ArtigoAdminDetalheDto MapearAdmin(Artigo a) => new(
        a.Id, a.Titulo, a.Slug, a.Resumo, a.Categoria, a.ImagemCapa, a.ImagemBanner, a.Autor, a.Destaque, a.Status,
        a.TempoLeituraMinutos, a.ArquivoPdfOrigem, a.DataCriacao, a.DataAtualizacao, a.DataPublicacao,
        MapearCapitulos(a));

    private static IEnumerable<string> QuebrarEmTermos(string? busca) =>
        string.IsNullOrWhiteSpace(busca)
            ? []
            : busca.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6);

    /// <summary>Escapa os curingas do LIKE (% e _) digitados pelo usuário.</summary>
    private static string EscaparLike(string termo) =>
        termo.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static int ContarPalavras(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? 0
            : texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
