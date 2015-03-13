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
				return "CiWong.OpenAPI.Agent";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{
			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "agent.default",
			   routeTemplate: "agent/{controller}/{action}",
			   defaults: new { controller = "agent", action = "index" },
			   constraints: new { controller = "(agent)" },
			   namespaces: new string[] { "CiWong.OpenAPI.Agent.Controllers" }
		   );
		}
	}
}
