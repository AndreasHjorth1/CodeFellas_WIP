namespace Prosjekt.Services
{
    public static class SecurityHeaders
    {
        public const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https://*.tile.openstreetmap.org https://cache.kartverket.no https://server.arcgisonline.com https://cdnjs.cloudflare.com; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "connect-src 'self' https://nominatim.openstreetmap.org; " +
            "object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["X-XSS-Protection"] = "1; mode=block";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
                await next();
            });
        }
    }
}
