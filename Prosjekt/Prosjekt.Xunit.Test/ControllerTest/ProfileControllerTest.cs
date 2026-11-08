using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Xunit;

namespace Prosjekt.Xunit
{
    public class ProfileControllerTest
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            var controller = new ProfileController();

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}