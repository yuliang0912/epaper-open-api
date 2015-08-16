using System.Web.Http;

namespace CiWong.OpenAPI.Web.Controllers
{
	public class HomeController : ApiController
	{
		[HttpGet, HttpPost]
		public dynamic Index()
		{
			return "hello";
		}
	}
}