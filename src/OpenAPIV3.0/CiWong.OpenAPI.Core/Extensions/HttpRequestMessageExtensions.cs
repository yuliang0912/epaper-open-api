using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.Core.Extensions
{
	public static class HttpRequestMessageExtensions
	{
		public static string GetClientIp(this HttpRequestMessage request)
		{
			if (request.Properties.ContainsKey("MS_HttpContext"))
			{
				return ((dynamic)request.Properties["MS_HttpContext"]).Request.UserHostAddress as string;
			}
			else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
			{
				RemoteEndpointMessageProperty prop;
				prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
				return prop.Address;
			}
			else
			{
				throw new Exception("Could not get client IP");
			}
		}

		/// <summary>
		/// 获取body中的数据
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static string GetBodyContent(this HttpRequestMessage request)
		{
			var result = request.Content.ReadAsStringAsync();

			result.Wait();

			if (result.IsFaulted || result.IsCanceled || !result.IsCompleted)
			{
				return null;
			}
			return result.Result;
		}

		public static NameValueCollection GetParamsFromBody(this HttpRequestMessage request)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();

			var result = request.Content.ReadAsStringAsync();
			result.Wait();

			if (result.IsFaulted || result.IsCanceled || !result.IsCompleted || string.IsNullOrWhiteSpace(result.Result))
			{
				return nameValueCollection;
			}

			string content = result.Result;

			Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);

			MatchCollection mc = re.Matches(content);

			foreach (Match item in mc)
			{
				nameValueCollection.Add(item.Result("$2"), item.Result("$3"));
			}

			return nameValueCollection;
		}

		public static string GetParamValueFromBody(this HttpRequestMessage request, string name)
		{
			var result = request.Content.ReadAsStringAsync();
			result.Wait();

			if (result.IsFaulted || result.IsCanceled || !result.IsCompleted || string.IsNullOrWhiteSpace(result.Result))
			{
				return null;
			}

			string content = result.Result;

			Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);

			MatchCollection mc = re.Matches(content);

			foreach (Match item in mc)
			{
				if (item.Result("$2").Equals(name.Trim()))
				{
					return item.Result("$3");
				}
			}

			return null;
		}
	}
}
