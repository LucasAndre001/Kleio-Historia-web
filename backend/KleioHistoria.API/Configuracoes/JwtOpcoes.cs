using System.ComponentModel.DataAnnotations;

namespace KleioHistoria.API.Configuracoes;

/// <summary>
/// Configurações do token JWT, lidas da seção "Jwt" (appsettings ou variáveis
/// de ambiente como <c>Jwt__Chave</c>). A chave NUNCA fica no código-fonte.
/// </summary>
public class JwtOpcoes
{
    public const string Secao = "Jwt";

    [Required(ErrorMessage = "Defina Jwt__Chave (variável JWT_CHAVE no arquivo .env).")]
    [MinLength(32, ErrorMessage = "Jwt__Chave precisa ter pelo menos 32 caracteres.")]
    public string Chave { get; set; } = string.Empty;

    [Required]
    public string Emissor { get; set; } = "kleio-historia-api";

    [Required]
    public string Audiencia { get; set; } = "kleio-historia-web";

    [Range(5, 1440)]
    public int ExpiracaoMinutos { get; set; } = 120;
}
