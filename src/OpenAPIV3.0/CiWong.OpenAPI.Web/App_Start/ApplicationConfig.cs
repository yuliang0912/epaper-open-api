using Autofac;
using Autofac.Integration.WebApi;
using Autofac.Integration.Mvc;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Filter;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using CiWong.Framework.Plugin;

namespace CiWong.OpenAPI.Web.App_Start
{
	public class ApplicationConfig
	{
		/// <summary>
		/// 注册全局异常过滤器
		/// </summary>
		public static void RegisterFilters()
		{
			GlobalConfiguration.Configuration.Filters.Add(new ApiExceptionAttribute());
		}

		/// <summary>
		/// 注册依赖注入
		/// </summary>
		public static void RegisterIOC()
		{
			var builder = new ContainerBuilder();
			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
			builder.RegisterControllers(Assembly.GetExecutingAssembly());//注册mvc容器的实现

			#region autoFac注入
			builder.RegisterModule<CiWong.OpenAPI.BookCase.BookCaseModule>();
			builder.RegisterModule<CiWong.OpenAPI.ExpandWork.ResourceModule>();
			builder.RegisterModule<CiWong.OpenAPI.YiShang.YiShangModule>();
			builder.RegisterModule<CiWong.OpenAPI.Work.WorkModule>();
			#endregion

			Assembly[] asm = PluginManager.GetAllAssembly("CiWong.OpenAPI.*.dll").ToArray();
			builder.RegisterAssemblyTypes(asm);

			var container = builder.Build();
			GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
			DependencyResolver.SetResolver(new AutofacDependencyResolver(container));//注册MVC容器
		}


		/// <summary>
		/// 注册API路由
		/// </summary>
		public static void RegisterRoute()
		{
			System.Web.Routing.RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			GlobalConfiguration.Configuration.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{action}",
				defaults: new { controller = "Home", action = "Index" }
			);

			//使web api支持namespace
			GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerSelector), new NamespaceHttpControllerSelector(GlobalConfiguration.Configuration));
		}

		/// <summary>
		/// 注册Json序列化
		/// </summary>
		public static void RegisterJsonFormatter()
		{
			//定义Json序列化方式
			GlobalConfiguration.Configuration.Formatters.Insert(0, new CustomJsonFormatter());
			//移除默认的XML序列化
			GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Remove(new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml"));
		}
	}
}