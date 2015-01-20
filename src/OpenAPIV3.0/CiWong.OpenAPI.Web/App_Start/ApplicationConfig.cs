﻿using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Filter;
using System.Web.Http;
using System.Web.Http.Dispatcher;

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
		/// 注册API路由
		/// </summary>
		public static void RegisterRoute()
		{
			
			GlobalConfiguration.Configuration.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "{controller}/{action}"
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