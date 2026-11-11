using KleioHistoria.API.Excecoes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KleioHistoria.API.Infraestrutura;

/// <summary>
/// Converte qualquer exceção em uma resposta padronizada no formato
/// ProblemDetails (RFC 9457), que o frontend sabe interpretar.
///
/// - Exceções de negócio (<see cref="KleioExcecao"/>) viram 4xx com a mensagem original.
/// - Erros inesperados viram 500 com mensagem genérica (detalhes só no log),
///   para não expor informações internas ao visitante.
/// </summary>
public sealed class TratadorGlobalExcecoes(
    ILogger<TratadorGlobalExcecoes> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        int status;
        string titulo;
        string detalhe;

        if (excecao is KleioExcecao kleio)
        {
            status = kleio.StatusCode;
            titulo = kleio.Titulo;
            detalhe = kleio.Message;
            logger.LogInformation("Requisição recusada ({Status}) em {Caminho}: {Mensagem}",
                status, contexto.Request.Path, kleio.Message);
        }
        else if (excecao is BadHttpRequestException badRequest)
        {
            // Ex.: corpo maior que o limite configurado.
            status = badRequest.StatusCode;
            titulo = "Requisição inválida";
            detalhe = status == StatusCodes.Status413PayloadTooLarge
                ? "O arquivo enviado excede o tamanho máximo permitido."
                : "A requisição não pôde ser processada.";
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            titulo = "Erro interno";
            detalhe = "Ocorreu um erro inesperado. Tente novamente em instantes.";
            logger.LogError(excecao, "Erro não tratado em {Metodo} {Caminho}",
                contexto.Request.Method, contexto.Request.Path);
        }

        contexto.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = excecao,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = detalhe
            }
        });
    }
}
