using System.Text.Json;
using System.Text.Json.Serialization;
using KleioHistoria.API.Data;
using KleioHistoria.API.Infraestrutura;

// =====================================================================
//  Kleio História — API
//  Program.cs monta a aplicação em duas etapas:
//   1) registrar serviços (injeção de dependência);
//   2) montar o pipeline HTTP (middlewares, na ordem em que executam).
// =====================================================================

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- 1) Serviços ----------
builder.Services
    .AddKleioOpcoes(config)
    .AddKleioBancoDeDados(config)
    .AddKleioIdentidade()
    .AddKleioAutenticacaoJwt(config)
    .AddKleioServicos()
    .AddKleioSwagger()
    .AddKleioCors(config)
    .AddKleioLimiteRequisicoes();

builder.Services.AddControllers()
    .AddJsonOptions(opcoes =>
        // Enums trafegam como texto: "rascunho" / "publicado".
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorGlobalExcecoes>();

var app = builder.Build();

// ---------- 2) Pipeline HTTP ----------
app.UseForwardedHeaders();
app.UseExceptionHandler();   // captura exceções e responde ProblemDetails

if (config.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Kleio História API v1");
        opcoes.DocumentTitle = "Kleio História — Swagger";
    });
}

app.UseKleioUploads();       // /uploads/imagens/*
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();     // lê o JWT e identifica o usuário
app.UseAuthorization();      // aplica [Authorize]

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Aguarda o PostgreSQL, aplica migrations e cria o administrador/dados de demonstração.
await DbInicializador.InicializarAsync(app.Services, app.Lifetime.ApplicationStopping);

await app.RunAsync();

/// <summary>Torna a classe Program visível para testes de integração.</summary>
public partial class Program;
