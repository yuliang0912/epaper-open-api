using System;
using System.Net;

namespace CiWong.OpenAPI.Core
{
	/// <summary>
	/// API异常基类
	/// </summary>
	public class ApiException : Exception
	{
		public ApiException(RetEum ret, int errCode, string message, HttpStatusCode status = HttpStatusCode.OK)
		{
			this.Ret = ret;
			this.ErrCode = errCode;
			this.Message = message;
			this.HttpStatus = status;
		}

		/// <summary>
		/// 服务器状态码
		/// </summary>
		public RetEum Ret { get; set; }

		/// <summary>
		/// 错误码
		/// </summary>
		public int ErrCode { get; set; }

		/// <summary>
		/// 错误信息
		/// </summary>
		public new string Message { get; set; }

		/// <summary>
		/// Http状态码
		/// </summary>
		public HttpStatusCode HttpStatus { get; set; }
	}

	/// <summary>
	/// 参数异常
	/// </summary>
	public class ApiArgumentException : ApiException
	{
		public ApiArgumentException(string message, int errCode = 1) : base(RetEum.ArgumentError, errCode, message) { }
	}

	/// <summary>
	/// 空参数异常
	/// </summary>
	public class ApiArgumentNullException : ApiException
	{
		public ApiArgumentNullException(string message, int errCode = 2) : base(RetEum.ArgumentError, errCode, message) { }
	}

	/// <summary>
	/// 数据格式异常
	/// </summary>
	public class ApiFormatException : ApiException
	{
		public ApiFormatException(string message = "数据格式错误", int errCode = 1) : base(RetEum.ApplicationError, errCode, message) { }
	}

	/// <summary>
	/// 数组/集合下标越界异常
	/// </summary>
	public class ApiIndexOutOfException : ApiException
	{
		public ApiIndexOutOfException(string message = "下标越界错误", int errCode = 1) : base(RetEum.ApplicationError, errCode, message) { }
	}

	/// <summary>
	/// 鉴权失败
	/// </summary>
	public class ApiAuthenticationException : ApiException
	{
		public ApiAuthenticationException(string message = "鉴权失败", int errCode = 1) : base(RetEum.AuthenticationFailure, errCode, message) { }
	}
}
