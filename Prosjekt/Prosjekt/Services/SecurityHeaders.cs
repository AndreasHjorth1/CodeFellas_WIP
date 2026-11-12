using System.Security.Cryptography;

namespace Prosjekt.Services
{
    /// <summary>
    /// Legger sikkerhetsheadere på alle svar fra applikasjonen.
    /// </summary>
    public static class SecurityHeaders
    {
        private const string NonceKey = "csp-nonce";

        /// <summary>
        /// Hvor nettleseren får hente innhold fra. Nye eksterne adresser må legges til her.
        /// ogc.dsb.no: kartet over offentlige tilfluktsrom på Kriseinformasjon-siden henter data
        /// (connect-src) og reservekartlaget som bilde (img-src) derfra.
        /// valhalla1.openstreetmap.de: Match-siden henter kjørerute langs veien fra ressurs til behov (connect-src).
        /// Inline-skript kjører bare med riktig nonce. Stiler beholder 'unsafe-inline', fordi nonce ikke dekker style-attributter.
        /// </summary>
        public static string ContentSecurityPolicy(string nonce) =>
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https://*.tile.openstreetmap.org https://cache.kartverket.no https://server.arcgisonline.com https://cdnjs.cloudflare.com https://ogc.dsb.no; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "connect-src 'self' https://nominatim.openstreetmap.org https://ogc.dsb.no https://valhalla1.openstreetmap.de; " +
            "object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        /// <summary>
        /// Nonce-en for denne forespørselen. Brukes på inline-skript: &lt;script nonce="@Context.CspNonce()"&gt;
        /// </summary>
        public static string CspNonce(this HttpContext context) =>
            context.Items[NonceKey] as string ?? string.Empty;

        /// <summary>
        /// Setter headerne før siden lages. Referrer-Policy kan ikke være no-referrer, for da kan kartet slutte å virke.
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                /// Ny tilfeldig verdi for hver forespørsel, så en angriper ikke kan gjette den
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                context.Items[NonceKey] = nonce;

                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["X-XSS-Protection"] = "1; mode=block";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Content-Security-Policy"] = ContentSecurityPolicy(nonce);
                await next();
            });
        }
    }
}
