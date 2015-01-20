using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CiWong.OpenAPI.Core.Extensions
{
	public class HttpResponseMessageHelper
	{
		public static HttpResponseMessage CreateResponse(HttpStatusCode httpStatusCode = HttpStatusCode.OK)
		{
			var response = new HttpResponseMessage(httpStatusCode)
			{
				Content = new StringContent("fds")
			};

			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			return response;
		}
	}
}
