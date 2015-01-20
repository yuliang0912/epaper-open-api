using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Web.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace CiWong.OpenAPI.Web
{
	// 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
	// 请访问 http://go.microsoft.com/?LinkId=9394801
	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			ApplicationConfig.RegisterFilters();
			ApplicationConfig.RegisterRoute();
			ApplicationConfig.RegisterJsonFormatter();
		}
	}
}