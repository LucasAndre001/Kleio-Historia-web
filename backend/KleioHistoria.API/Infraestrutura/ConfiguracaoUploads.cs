using KleioHistoria.API.Configuracoes;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace KleioHistoria.API.Infraestrutura;

public static class ConfiguracaoUploads
{
    /// <summary>
    /// Serve as imagens enviadas como arquivos estáticos em /uploads/imagens.
    /// A pasta de PDFs NÃO é exposta: os originais ficam só no volume.
    /// </summary>
    public static WebApplication UseKleioUploads(this WebApplication app)
    {
        var opcoes = app.Services.GetRequiredService<IOptions<ArmazenamentoOpcoes>>().Value;
        var raiz = Path.GetFullPath(opcoes.CaminhoRaiz);
        var pastaImagens = Path.Combine(raiz, ArmazenamentoOpcoes.PastaImagens);
        Directory.CreateDirectory(pastaImagens);
        Directory.CreateDirectory(Path.Combine(raiz, ArmazenamentoOpcoes.PastaPdfs));

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(pastaImagens),
            RequestPath = ArmazenamentoOpcoes.UrlPublicaImagens,
            OnPrepareResponse = contexto =>
            {
                // Impede o navegador de "adivinhar" outro tipo de conteúdo.
                contexto.Context.Response.Headers.XContentTypeOptions = "nosniff";
                contexto.Context.Response.Headers.CacheControl = "public, max-age=604800";
            }
        });

        app.Logger.LogInformation("Uploads armazenados em {Raiz}", raiz);
        return app;
    }
}
