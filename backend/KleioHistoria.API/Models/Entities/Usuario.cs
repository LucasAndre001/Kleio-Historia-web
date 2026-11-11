using Microsoft.AspNetCore.Identity;

namespace KleioHistoria.API.Models.Entities;

/// <summary>
/// Usuário administrador (professor).
///
/// Herda de <see cref="IdentityUser{TKey}"/>, que já fornece os campos pedidos no
/// modelo: <c>Id</c>, <c>Email</c> e <c>PasswordHash</c> (o "SenhaHash", gerado pelo
/// Identity com PBKDF2 + salt). O "Role" é guardado nas tabelas de perfis do
/// Identity (Perfis / UsuarioPerfis), o que permite mais de um perfil por usuário.
/// </summary>
public class Usuario : IdentityUser<int>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Artigos criados por este usuário.</summary>
    public ICollection<Artigo> Artigos { get; set; } = new List<Artigo>();
}
