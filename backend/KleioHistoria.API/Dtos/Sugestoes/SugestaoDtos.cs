using System.ComponentModel.DataAnnotations;

namespace KleioHistoria.API.Dtos.Sugestoes;

/// <summary>Formulário público de sugestão de tema.</summary>
public class SugestaoCriarDto
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Opcional. Aceita dígitos, espaços, parênteses, + e -.</summary>
    [RegularExpression(@"^[0-9()+\-\s]{8,30}$", ErrorMessage = "Telefone inválido.")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Informe o tema sugerido.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "O tema deve ter entre 3 e 200 caracteres.")]
    public string TemaSugerido { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "A mensagem deve ter no máximo 2000 caracteres.")]
    public string? Mensagem { get; set; }
}

/// <summary>Confirmação devolvida ao visitante (sem ecoar seus dados pessoais).</summary>
public record SugestaoConfirmacaoDto(int Id, DateTime DataEnvio);

/// <summary>Sugestão completa, visível apenas no painel administrativo.</summary>
public record SugestaoDto(
    int Id,
    string Nome,
    string Email,
    string? Telefone,
    string TemaSugerido,
    string? Mensagem,
    DateTime DataEnvio);
