using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.Store
{
	public class StoreAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.YiShang";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "yishang.default",
			   routeTemplate: "yishang/{controller}/{action}",
			   defaults: new { controller = "yishang", action = "index" },
			   constraints: new { controller = "(yishang)" },
			   namespaces: new string[] { "CiWong.OpenAPI.YiShang.Controllers" }
		   );
		}
	}
}
