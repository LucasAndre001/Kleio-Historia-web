namespace KleioHistoria.API.Infraestrutura;

/// <summary>Nomes de perfis (roles) usados em [Authorize(Roles = ...)].</summary>
public static class Perfis
{
    public const string Admin = "Admin";
}

/// <summary>Nomes das políticas de limite de requisições (proteção contra abuso).</summary>
public static class PoliticasLimite
{
    public const string Login = "login";
    public const string Sugestoes = "sugestoes";
}

/// <summary>
/// Limite do corpo HTTP nos endpoints de upload. O limite fino por tipo
/// (5 MB imagem, 20 MB PDF) é verificado nos Services, com mensagem amigável.
/// </summary>
public static class LimitesUpload
{
    public const long CorpoMaximoBytes = 25L * 1024 * 1024;
}
