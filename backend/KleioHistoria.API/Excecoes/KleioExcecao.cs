namespace KleioHistoria.API.Excecoes;

/// <summary>
/// Exceção base das regras de negócio. Cada subclasse define o código HTTP
/// que o tratador global (<c>TratadorGlobalExcecoes</c>) devolverá ao cliente.
/// Assim os Services não precisam conhecer HTTP, e os Controllers ficam enxutos.
/// </summary>
public abstract class KleioExcecao(string mensagem, int statusCode, string titulo) : Exception(mensagem)
{
    public int StatusCode { get; } = statusCode;
    public string Titulo { get; } = titulo;
}

/// <summary>400 — a requisição viola uma regra de negócio.</summary>
public class RegraNegocioExcecao(string mensagem)
    : KleioExcecao(mensagem, StatusCodes.Status400BadRequest, "Regra de negócio violada");

/// <summary>404 — o recurso pedido não existe (ou não está publicado).</summary>
public class RecursoNaoEncontradoExcecao(string mensagem)
    : KleioExcecao(mensagem, StatusCodes.Status404NotFound, "Recurso não encontrado");

/// <summary>409 — conflito, por exemplo slug já utilizado.</summary>
public class ConflitoExcecao(string mensagem)
    : KleioExcecao(mensagem, StatusCodes.Status409Conflict, "Conflito");

/// <summary>
/// 415/422 — arquivo enviado é inválido (tipo errado, corrompido, sem texto...).
/// </summary>
public class ArquivoInvalidoExcecao(string mensagem, int statusCode = StatusCodes.Status422UnprocessableEntity)
    : KleioExcecao(mensagem, statusCode, "Arquivo inválido");
