using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var auth = ctx.Request.Headers["Authorization"].ToString();
        if (auth.StartsWith("Bearer "))
        {
            var token = auth.Replace("Bearer ", "");
            var ok = await db.UserSessions.AnyAsync(s => s.Token == token
                && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);
            if (!ok)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = new { code = "SESSION_INVALID", message = "Session expired or revoked" }
                });
                return;
            }
        }
        await _next(ctx);
    }
}