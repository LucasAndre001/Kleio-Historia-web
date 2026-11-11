using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace KleioHistoria.API.Infraestrutura;

public static class ClaimsPrincipalExtensoes
{
    /// <summary>Lê o Id do usuário autenticado a partir da claim "sub" do JWT.</summary>
    public static int ObterUsuarioId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var id)
            ? id
            : throw new UnauthorizedAccessException("Token sem identificação do usuário.");
    }
}
