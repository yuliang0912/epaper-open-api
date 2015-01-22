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
				return "CiWong.OpenAPI.Store";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "store.default",
			   routeTemplate: "store/{controller}/{action}",
			   defaults: new { controller = "store", action = "index" },
			   constraints: new { controller = "(store)" },
			   namespaces: new string[] { "CiWong.OpenAPI.Store.Controllers" }
		   );
		}
	}
}
