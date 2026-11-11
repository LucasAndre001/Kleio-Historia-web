using System.ComponentModel.DataAnnotations;

namespace KleioHistoria.API.Configuracoes;

/// <summary>
/// Dados do administrador de demonstração criado na primeira inicialização.
/// Os valores vêm do arquivo .env (ADMIN_EMAIL, ADMIN_SENHA, ADMIN_NOME).
/// </summary>
public class AdminOpcoes
{
    public const string Secao = "Admin";

    [Required(ErrorMessage = "Defina Admin__Email (ADMIN_EMAIL no .env).")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Defina Admin__Senha (ADMIN_SENHA no .env).")]
    public string Senha { get; set; } = string.Empty;

    public string Nome { get; set; } = "Professor Administrador";
}
