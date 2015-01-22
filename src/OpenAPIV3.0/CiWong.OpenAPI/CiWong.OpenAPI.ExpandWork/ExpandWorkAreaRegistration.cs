using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.Store
{
	public class ExpandWork : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.ExpandWork";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{
			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "expandwork.default",
			   routeTemplate: "expandwork/{controller}/{action}",
			   defaults: new { controller = "expandwork", action = "index" },
			   constraints: new { controller = "(expandwork)" },
			   namespaces: new string[] { "CiWong.OpenAPI.ExpandWork.Controllers" }
		   );
		}
	}
}
