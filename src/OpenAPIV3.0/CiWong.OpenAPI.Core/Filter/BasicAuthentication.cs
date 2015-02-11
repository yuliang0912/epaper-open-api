using System;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CiWong.OpenAPI.Core
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class BasicAuthentication : AuthorizationFilterAttribute
	{
		protected int userId = 0;
		public BasicAuthentication() { }

		public override void OnAuthorization(HttpActionContext actionContext)
		{
			var identity = ParseAuthorizationHeader(actionContext);

			if (null == identity || !int.TryParse(identity.Name, out userId))
			{
				actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.OK, new ApiResult()
				{
					Ret = RetEum.AuthenticationFailure,
					ErrorCode = 1,
					Message = "用户鉴权失败"
				});
				return;
			}
			if (userId < 1)
			{
				actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.OK, new ApiResult()
				{
					Ret = RetEum.AuthenticationFailure,
					ErrorCode = 2,
					Message = "用户鉴权失败"
				});
				return;
			}
			Thread.CurrentPrincipal = new GenericPrincipal(identity, null);
			base.OnAuthorization(actionContext);
		}

		protected virtual BasicAuthenticationIdentity ParseAuthorizationHeader(HttpActionContext actionContext)
		{
			string authHeader = null;
			var auth = actionContext.Request.Headers.Authorization;
			if (auth != null && auth.Scheme == "Basic")
			{
				authHeader = auth.Parameter;
			}
			if (string.IsNullOrEmpty(authHeader))
			{
				return null;
			}
			authHeader = Encoding.Default.GetString(Convert.FromBase64String(authHeader));
			var tokens = authHeader.Split(':');
			if (tokens.Length < 2)
			{
				return null;
			}
			return new BasicAuthenticationIdentity(tokens[0], tokens[1]);
		}

		public class BasicAuthenticationIdentity : GenericIdentity
		{
			public BasicAuthenticationIdentity(string name, string password)
				: base(name, "Basic")
			{
				this.Password = password;
			}

			/// <summary>  
			/// Basic Auth Password for custom authentication  
			/// </summary>  
			public string Password { get; set; }
		}
	}
}
