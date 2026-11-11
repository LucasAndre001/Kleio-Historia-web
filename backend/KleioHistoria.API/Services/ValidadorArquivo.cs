using System.Text;

namespace KleioHistoria.API.Services;

/// <summary>
/// Verifica o CONTEÚDO REAL dos arquivos pelos primeiros bytes ("magic numbers"),
/// em vez de confiar só na extensão ou no Content-Type enviados pelo navegador,
/// que podem ser falsificados facilmente.
/// </summary>
public static class ValidadorArquivo
{
    /// <summary>Tipos de imagem aceitos: extensão canônica → content-type.</summary>
    public static readonly IReadOnlyDictionary<string, string> TiposImagem = new Dictionary<string, string>
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    /// <summary>
    /// Detecta o tipo de imagem pela assinatura binária.
    /// Retorna a extensão canônica (".jpg", ".png"...) ou null se não for uma imagem aceita.
    /// SVG não é aceito de propósito: é XML e pode conter scripts.
    /// </summary>
    public static string? DetectarImagem(ReadOnlySpan<byte> cabecalho)
    {
        // JPEG: FF D8 FF
        if (cabecalho.Length >= 3 && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return ".jpg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (cabecalho.Length >= 8 && cabecalho[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ".png";

        // GIF: "GIF87a" ou "GIF89a"
        if (cabecalho.Length >= 6 && (cabecalho[..6].SequenceEqual("GIF87a"u8) || cabecalho[..6].SequenceEqual("GIF89a"u8)))
            return ".gif";

        // WEBP: "RIFF" ???? "WEBP"
        if (cabecalho.Length >= 12 && cabecalho[..4].SequenceEqual("RIFF"u8) && cabecalho[8..12].SequenceEqual("WEBP"u8))
            return ".webp";

        return null;
    }

    /// <summary>
    /// Um PDF começa com "%PDF-". A especificação tolera lixo antes do cabeçalho,
    /// por isso procuramos nos primeiros 1024 bytes.
    /// </summary>
    public static bool EhPdf(ReadOnlySpan<byte> cabecalho)
    {
        var limite = Math.Min(cabecalho.Length, 1024);
        return cabecalho[..limite].IndexOf("%PDF-"u8) >= 0;
    }

    /// <summary>Lê os primeiros bytes de um stream sem consumir o restante.</summary>
    public static async Task<byte[]> LerCabecalhoAsync(Stream stream, int quantidade, CancellationToken ct)
    {
        var buffer = new byte[quantidade];
        var lidos = 0;
        while (lidos < quantidade)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(lidos, quantidade - lidos), ct);
            if (n == 0) break;
            lidos += n;
        }
        return buffer[..lidos];
    }

    /// <summary>Texto curto e seguro para exibir nomes de arquivo enviados pelo usuário.</summary>
    public static string NomeSeguroParaExibicao(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "arquivo";
        var apenasNome = Path.GetFileName(nome);
        var sb = new StringBuilder(apenasNome.Length);
        foreach (var c in apenasNome)
            sb.Append(char.IsControl(c) ? '_' : c);
        var resultado = sb.ToString();
        return resultado.Length > 120 ? resultado[..120] : resultado;
    }
}
