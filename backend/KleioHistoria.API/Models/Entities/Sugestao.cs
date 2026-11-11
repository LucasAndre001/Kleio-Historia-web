namespace KleioHistoria.API.Models.Entities;

/// <summary>Sugestão de tema enviada por um visitante (sem login).</summary>
public class Sugestao
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string TemaSugerido { get; set; } = string.Empty;

    /// <summary>Detalhes opcionais (campo já existente no formulário da Home).</summary>
    public string? Mensagem { get; set; }

    public DateTime DataEnvio { get; set; }
}
