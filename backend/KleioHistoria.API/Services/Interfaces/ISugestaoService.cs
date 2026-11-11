using KleioHistoria.API.Dtos.Sugestoes;

namespace KleioHistoria.API.Services.Interfaces;

public interface ISugestaoService
{
    Task<SugestaoConfirmacaoDto> CriarAsync(SugestaoCriarDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<SugestaoDto>> ListarAsync(CancellationToken ct = default);
    Task ExcluirAsync(int id, CancellationToken ct = default);
}
