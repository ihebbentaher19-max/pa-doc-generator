using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PADocGenerator.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException(UserMessages.InvalidSession);

        return userId;
    }
}
