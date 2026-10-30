using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.ModelView;
using Xunit;

namespace Prosjekt.Xunit
{
    public class HomeControllerTest
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            var controller = new HomeController();

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var controller = new HomeController();

            var result = controller.Privacy();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewWithRequestId()
        {
            var httpContext = new DefaultHttpContext { TraceIdentifier = "test-trace-id" };
            var controller = new HomeController
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("test-trace-id", model.RequestId);
            Assert.True(model.ShowRequestId);
        }
    }
}