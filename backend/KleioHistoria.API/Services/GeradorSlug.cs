using System.Globalization;
using System.Text;

namespace KleioHistoria.API.Services;

/// <summary>
/// Converte um título em slug para URL.
/// Exemplo: "Revolução Francesa: 1789!" → "revolucao-francesa-1789".
/// </summary>
public static class GeradorSlug
{
    public const int TamanhoMaximo = 200;

    public static string Gerar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "artigo";

        // 1) FormD separa a letra do acento ("ç" vira "c" + "¸");
        //    depois descartamos os acentos (NonSpacingMark).
        var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        var ultimoFoiHifen = false;

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            var minuscula = char.ToLowerInvariant(c);

            // 2) Mantém só a-z e 0-9; todo o resto vira um único hífen.
            if (minuscula is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(minuscula);
                ultimoFoiHifen = false;
            }
            else if (!ultimoFoiHifen && sb.Length > 0)
            {
                sb.Append('-');
                ultimoFoiHifen = true;
            }
        }

        var slug = sb.ToString().Trim('-');

        // 3) Limita o tamanho sem cortar uma palavra ao meio, quando possível.
        if (slug.Length > TamanhoMaximo)
        {
            slug = slug[..TamanhoMaximo];
            var ultimoHifen = slug.LastIndexOf('-');
            if (ultimoHifen > TamanhoMaximo / 2)
                slug = slug[..ultimoHifen];
            slug = slug.Trim('-');
        }

        return slug.Length == 0 ? "artigo" : slug;
    }
}
