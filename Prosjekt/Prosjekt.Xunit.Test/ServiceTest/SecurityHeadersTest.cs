using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Prosjekt.Services;

namespace Prosjekt.Xunit.Test.ServiceTest
{
    public class SecurityHeadersTest
    {
        private static async Task<IHeaderDictionary> KjorPipeline()
        {
            var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            app.UseSecurityHeaders();
            var context = new DefaultHttpContext();

            await app.Build()(context);

            return context.Response.Headers;
        }

        [Theory]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "DENY")]
        [InlineData("X-XSS-Protection", "1; mode=block")]
        [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
        [InlineData("Content-Security-Policy", SecurityHeaders.ContentSecurityPolicy)]
        public async Task UseSecurityHeaders_SetsHeader(string navn, string forventet)
        {
            var headers = await KjorPipeline();

            Assert.Equal(forventet, headers[navn].ToString());
        }

        [Theory]
        [InlineData("default-src 'self'")]
        [InlineData("object-src 'none'")]
        [InlineData("frame-ancestors 'none'")]
        [InlineData("base-uri 'self'")]
        [InlineData("form-action 'self'")]
        public void ContentSecurityPolicy_ContainsDirective(string direktiv)
        {
            Assert.Contains(direktiv, SecurityHeaders.ContentSecurityPolicy);
        }
    }
}
