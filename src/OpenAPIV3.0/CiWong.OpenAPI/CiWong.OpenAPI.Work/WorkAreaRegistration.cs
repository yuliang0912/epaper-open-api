using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.Store
{
	public class WorkAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.Work";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "work.default",
			   routeTemplate: "work/{controller}/{action}",
			   defaults: new { controller = "work", action = "index" },
			   constraints: new { controller = "(work)" },
			   namespaces: new string[] { "CiWong.OpenAPI.Work.Controllers" }
		   );
		}
	}
}
