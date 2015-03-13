using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.Class
{
	public class ClassAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.Class";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "class.default",
			   routeTemplate: "class/{controller}/{action}",
			   defaults: new { controller = "class", action = "index" },
			   constraints: new { controller = "(class)" },
			   namespaces: new string[] { "CiWong.OpenAPI.Class.Controllers" }
		   );
		}
	}
}
