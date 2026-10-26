//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Prosjekt.Controllers;
//using Prosjekt.Models.ModelView;

//namespace Prosjekt.Xunit.Test.ControllerTest
//{
//    public class HomeControllerTest
//    {
//        private static HomeController CreateController()
//        {
//            var config = new ConfigurationBuilder().Build();
//            return new HomeController(config);
//        }

//        [Fact]
//        public void Index_ReturnsViewResult()
//        {
//            var controller = CreateController();

//            var result = controller.Index();

//            Assert.IsType<ViewResult>(result);
//        }

//        [Fact]
//        public void Privacy_ReturnsViewResult()
//        {
//            var controller = CreateController();

//            var result = controller.Privacy();

//            Assert.IsType<ViewResult>(result);
//        }

//        [Fact]
//        public void Error_ReturnsViewWithRequestId()
//        {
//            // Arrange
//            var controller = CreateController();
//            controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext()
//            };

//            // Act
//            var result = controller.Error();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
//            Assert.False(string.IsNullOrEmpty(model.RequestId));
//        }
//    }
//}