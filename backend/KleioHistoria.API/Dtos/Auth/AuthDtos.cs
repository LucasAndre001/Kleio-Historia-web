using System.ComponentModel.DataAnnotations;

namespace KleioHistoria.API.Dtos.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Resposta do login: o token e quando ele expira (UTC).</summary>
public record LoginRespostaDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);

public record UsuarioDto(int Id, string Nome, string Email, IReadOnlyList<string> Perfis);
