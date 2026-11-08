using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Prosjekt.Xunit.Test.Fakes
{
    /// <summary>
    /// TempData uten session eller cookies, så controllere som bruker TempData kan testes.
    /// </summary>
    public class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}