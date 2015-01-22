using System.Web.Mvc;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.Store
{
	public class BookCaseAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "CiWong.OpenAPI.BookCase";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{

			GlobalConfiguration.Configuration.Routes.MapHttpRouteWithNameSpace(
			   name: "bookcase.default",
			   routeTemplate: "bookcase/{controller}/{action}",
			   defaults: new { controller = "bookcase", action = "index" },
			   constraints: new { controller = "(bookcase)" },
			   namespaces: new string[] { "CiWong.OpenAPI.BookCase.Controllers" }
		   );
		}
	}
}
