using KleioHistoria.API.Data;
using KleioHistoria.API.Dtos.Sugestoes;
using KleioHistoria.API.Excecoes;
using KleioHistoria.API.Models.Entities;
using KleioHistoria.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KleioHistoria.API.Services;

public class SugestaoService(AppDbContext db, ILogger<SugestaoService> logger) : ISugestaoService
{
    public async Task<SugestaoConfirmacaoDto> CriarAsync(SugestaoCriarDto dto, CancellationToken ct = default)
    {
        var sugestao = new Sugestao
        {
            Nome = dto.Nome.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
            TemaSugerido = dto.TemaSugerido.Trim(),
            Mensagem = string.IsNullOrWhiteSpace(dto.Mensagem) ? null : dto.Mensagem.Trim(),
            DataEnvio = DateTime.UtcNow
        };

        db.Sugestoes.Add(sugestao);
        await db.SaveChangesAsync(ct);

        // Logamos apenas o Id: nome, e-mail e telefone são dados pessoais.
        logger.LogInformation("Sugestão {Id} recebida", sugestao.Id);
        return new SugestaoConfirmacaoDto(sugestao.Id, sugestao.DataEnvio);
    }

    public async Task<IReadOnlyList<SugestaoDto>> ListarAsync(CancellationToken ct = default)
    {
        return await db.Sugestoes.AsNoTracking()
            .OrderByDescending(s => s.DataEnvio)
            .Select(s => new SugestaoDto(s.Id, s.Nome, s.Email, s.Telefone, s.TemaSugerido, s.Mensagem, s.DataEnvio))
            .ToListAsync(ct);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var removidas = await db.Sugestoes.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
        if (removidas == 0)
            throw new RecursoNaoEncontradoExcecao($"Sugestão {id} não encontrada.");
    }
}
