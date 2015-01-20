using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Text;
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
	}
}
