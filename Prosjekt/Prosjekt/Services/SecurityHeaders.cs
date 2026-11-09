namespace Prosjekt.Services
{
    /// <summary>
    /// Legger sikkerhetsheadere på alle svar fra applikasjonen.
    /// </summary>
    public static class SecurityHeaders
    {
        /// <summary>
        /// Hvor nettleseren får hente innhold fra. Nye eksterne adresser må legges til her.
        /// ogc.dsb.no: kartet over offentlige tilfluktsrom på Kriseinformasjon-siden henter data
        /// (connect-src) og reservekartlaget som bilde (img-src) derfra.
        /// </summary>
        public const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https://*.tile.openstreetmap.org https://cache.kartverket.no https://server.arcgisonline.com https://cdnjs.cloudflare.com https://ogc.dsb.no; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "connect-src 'self' https://nominatim.openstreetmap.org https://ogc.dsb.no; " +
            "object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        /// <summary>
        /// Setter headerne før siden lages. Referrer-Policy kan ikke være no-referrer, for da kan kartet slutte å virke.
        /// </summary>
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
