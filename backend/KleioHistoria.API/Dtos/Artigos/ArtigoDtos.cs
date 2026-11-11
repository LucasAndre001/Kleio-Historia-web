using KleioHistoria.API.Dtos.Capitulos;
using KleioHistoria.API.Models.Entities;

namespace KleioHistoria.API.Dtos.Artigos;

// DTOs de SAÍDA são "records": imutáveis e com igualdade por valor.
// Eles definem exatamente o que a API expõe, sem vazar campos internos
// (ex.: UsuarioId, ConteudoTexto).

/// <summary>Card de artigo na listagem pública.</summary>
public record ArtigoResumoDto(
    int Id,
    string Titulo,
    string Slug,
    string Resumo,
    string Categoria,
    string? ImagemCapa,
    string? ImagemBanner,
    string? Autor,
    DateTime? DataPublicacao,
    int TempoLeituraMinutos,
    bool Destaque);

/// <summary>Artigo completo para a tela de leitura.</summary>
public record ArtigoDetalheDto(
    int Id,
    string Titulo,
    string Slug,
    string Resumo,
    string Categoria,
    string? ImagemCapa,
    string? Autor,
    DateTime? DataPublicacao,
    int TempoLeituraMinutos,
    IReadOnlyList<CapituloDto> Capitulos);

/// <summary>Linha da listagem administrativa (inclui rascunhos).</summary>
public record ArtigoAdminResumoDto(
    int Id,
    string Titulo,
    string Slug,
    string Categoria,
    string? ImagemCapa,
    StatusArtigo Status,
    bool Destaque,
    int TotalCapitulos,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    DateTime? DataPublicacao);

/// <summary>Artigo completo para edição e pré-visualização no painel.</summary>
public record ArtigoAdminDetalheDto(
    int Id,
    string Titulo,
    string Slug,
    string Resumo,
    string Categoria,
    string? ImagemCapa,
    string? ImagemBanner,
    string? Autor,
    bool Destaque,
    StatusArtigo Status,
    int TempoLeituraMinutos,
    string? ArquivoPdfOrigem,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    DateTime? DataPublicacao,
    IReadOnlyList<CapituloDto> Capitulos);
