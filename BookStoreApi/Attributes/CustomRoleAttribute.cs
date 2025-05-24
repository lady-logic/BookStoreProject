using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookStoreApi.Attributes;

public class CustomRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _role;

    public CustomRoleAttribute(string role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Zuerst prüfen, ob der Benutzer authentifiziert ist
        if (!context.HttpContext.User.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Nach der Rolle suchen - AccountController setzt "role" als Claim-Typ
        var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        // Debug-Output (kann später entfernt werden)
        Console.WriteLine($"[DEBUG] Looking for role: {_role}, Found: {roleClaim ?? "null"}");
        Console.WriteLine($"[DEBUG] All claims: {string.Join(", ", context.HttpContext.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");

        if (string.IsNullOrEmpty(roleClaim) || roleClaim != _role)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}