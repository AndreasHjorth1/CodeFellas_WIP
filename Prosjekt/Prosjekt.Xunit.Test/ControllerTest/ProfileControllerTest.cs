using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Xunit;

namespace Prosjekt.Xunit
{
    public class InnstillingerControllerTest
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            var controller = new SettingsController();

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}