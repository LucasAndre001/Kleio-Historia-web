using KleioHistoria.API.Dtos.Artigos;

namespace KleioHistoria.API.Services.Interfaces;

public interface IArtigoService
{
    // ---- Área pública: somente artigos PUBLICADOS ----
    Task<IReadOnlyList<ArtigoResumoDto>> ListarPublicadosAsync(FiltroArtigosDto filtro, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListarCategoriasAsync(CancellationToken ct = default);
    Task<ArtigoDetalheDto> ObterPublicadoPorSlugAsync(string slug, CancellationToken ct = default);

    // ---- Área administrativa: todos os artigos ----
    Task<IReadOnlyList<ArtigoAdminResumoDto>> ListarTodosAsync(CancellationToken ct = default);
    Task<ArtigoAdminDetalheDto> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<ArtigoAdminDetalheDto> CriarAsync(ArtigoSalvarDto dto, int usuarioId, CancellationToken ct = default);
    Task<ArtigoAdminDetalheDto> AtualizarAsync(int id, ArtigoSalvarDto dto, CancellationToken ct = default);
    Task ExcluirAsync(int id, CancellationToken ct = default);
    Task<ArtigoAdminDetalheDto> PublicarAsync(int id, CancellationToken ct = default);
    Task<ArtigoAdminDetalheDto> DespublicarAsync(int id, CancellationToken ct = default);
    Task DefinirDestaqueAsync(int id, bool destaque, CancellationToken ct = default);
}
