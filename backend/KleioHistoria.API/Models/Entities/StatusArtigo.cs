namespace KleioHistoria.API.Models.Entities;

/// <summary>
/// Situação editorial de um artigo. Somente artigos <see cref="Publicado"/>
/// aparecem nos endpoints públicos.
/// </summary>
public enum StatusArtigo
{
    Rascunho = 0,
    Publicado = 1
}
