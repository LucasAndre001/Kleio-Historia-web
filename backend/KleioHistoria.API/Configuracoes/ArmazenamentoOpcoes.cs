namespace KleioHistoria.API.Configuracoes;

/// <summary>
/// Onde e como os arquivos enviados são guardados. No Docker, <see cref="CaminhoRaiz"/>
/// aponta para /app/uploads, que é um volume persistente.
/// </summary>
public class ArmazenamentoOpcoes
{
    public const string Secao = "Armazenamento";

    /// <summary>Pasta raiz dos uploads. Caminho relativo é resolvido a partir da pasta da API.</summary>
    public string CaminhoRaiz { get; set; } = "uploads";

    /// <summary>Tamanho máximo de imagens, em megabytes.</summary>
    public int TamanhoMaximoImagemMb { get; set; } = 5;

    /// <summary>Tamanho máximo de PDFs, em megabytes.</summary>
    public int TamanhoMaximoPdfMb { get; set; } = 20;

    /// <summary>Insere artigos demonstrativos quando o banco ainda não tem nenhum.</summary>
    public bool SemearDadosDemonstrativos { get; set; } = true;

    public const string PastaImagens = "imagens";
    public const string PastaPdfs = "pdfs";

    /// <summary>Prefixo público das imagens (servidas como arquivos estáticos).</summary>
    public const string UrlPublicaImagens = "/uploads/imagens";
}
