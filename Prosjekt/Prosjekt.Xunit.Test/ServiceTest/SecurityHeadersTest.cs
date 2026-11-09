using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Prosjekt.Services;

namespace Prosjekt.Xunit.Test.ServiceTest
{
    public class SecurityHeadersTest
    {
        private static async Task<HttpContext> KjorPipeline()
        {
            var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            app.UseSecurityHeaders();
            var context = new DefaultHttpContext();

            await app.Build()(context);

            return context;
        }

        private static string HentScriptSrc(string policy) =>
            policy.Split(';').Select(d => d.Trim()).Single(d => d.StartsWith("script-src"));

        [Theory]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "DENY")]
        [InlineData("X-XSS-Protection", "1; mode=block")]
        [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
        public async Task UseSecurityHeaders_SetsHeader(string navn, string forventet)
        {
            var context = await KjorPipeline();

            Assert.Equal(forventet, context.Response.Headers[navn].ToString());
        }

        [Fact]
        public async Task UseSecurityHeaders_SetsPolicyWithSameNonceAsViews()
        {
            var context = await KjorPipeline();

            var nonce = context.CspNonce();
            Assert.False(string.IsNullOrEmpty(nonce));
            Assert.Equal(SecurityHeaders.ContentSecurityPolicy(nonce), context.Response.Headers.ContentSecurityPolicy.ToString());
        }

        [Fact]
        public async Task UseSecurityHeaders_GivesNewNonceForEachRequest()
        {
            var forste = await KjorPipeline();
            var andre = await KjorPipeline();

            Assert.NotEqual(forste.CspNonce(), andre.CspNonce());
        }

        [Fact]
        public void ContentSecurityPolicy_ScriptSrcUsesNonceInsteadOfUnsafeInline()
        {
            var scriptSrc = HentScriptSrc(SecurityHeaders.ContentSecurityPolicy("abc123"));

            Assert.Contains("'nonce-abc123'", scriptSrc);
            Assert.DoesNotContain("'unsafe-inline'", scriptSrc);
        }

        [Theory]
        [InlineData("default-src 'self'")]
        [InlineData("object-src 'none'")]
        [InlineData("frame-ancestors 'none'")]
        [InlineData("base-uri 'self'")]
        [InlineData("form-action 'self'")]
        public void ContentSecurityPolicy_ContainsDirective(string direktiv)
        {
            Assert.Contains(direktiv, SecurityHeaders.ContentSecurityPolicy("abc123"));
        }
    }
}
