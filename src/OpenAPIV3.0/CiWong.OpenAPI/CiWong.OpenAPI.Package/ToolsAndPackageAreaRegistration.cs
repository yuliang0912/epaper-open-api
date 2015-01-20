using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.ToolsAndPackage
{
	public class ToolsAndPackageAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.Package";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "package.default",
			   routeTemplate: "package/{controller}/{action}",
			   defaults: new { controller = "package", action = "index" },
			   constraints: new { controller = "(package)" },
			   namespaces: new string[] { "CiWong.OpenAPI.ToolsAndPackage.Controllers" }
		   );

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "tools.default",
			   routeTemplate: "tools/{controller}/{action}",
			   defaults: new { controller = "tools", action = "index" },
			   constraints: new { controller = "(tools)" },
			   namespaces: new string[] { "CiWong.OpenAPI.ToolsAndPackage.Controllers" }
		   );
		}
	}
}
