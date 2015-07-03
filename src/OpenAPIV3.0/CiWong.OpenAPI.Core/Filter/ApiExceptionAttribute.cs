using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace CiWong.OpenAPI.Core.Filter
{
	public class ApiExceptionAttribute : ExceptionFilterAttribute
	{
		public override void OnException(HttpActionExecutedContext actionExecutedContext)
		{
			base.OnException(actionExecutedContext);
			Exception exception = actionExecutedContext.Exception;
			if (exception != null)
			{
				ApiException apiException = exception as ApiException;
				if (null == apiException)
				{
					apiException = new ApiException(RetEum.ServerError, ErrorCodeEum.ServiceError, exception.Message, HttpStatusCode.OK);
				}
				actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(apiException.HttpStatus, apiException);
			}
		}
	}
}
