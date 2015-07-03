using System;
using System.Net;

namespace CiWong.OpenAPI.Core
{
	/// <summary>
	/// API异常基类
	/// </summary>
	public class ApiException : Exception
	{
		public ApiException(RetEum ret = RetEum.Success, ErrorCodeEum errCode = ErrorCodeEum.Success, string message = "success", HttpStatusCode status = HttpStatusCode.OK)
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
		public ErrorCodeEum ErrCode { get; set; }

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
		public ApiArgumentException(ErrorCodeEum errCode, string message) : base(RetEum.Success, errCode, message) { }
	}

	///// <summary>
	///// 空参数异常
	///// </summary>
	//public class ApiArgumentNullException : ApiException
	//{
	//	public ApiArgumentNullException(string message, int errCode = 2) : base(RetEum.Success, errCode, message) { }
	//}

	///// <summary>
	///// 数据格式异常
	///// </summary>
	//public class ApiArgumentFormatException : ApiException
	//{
	//	public ApiArgumentFormatException(string message = "数据格式错误", int errCode = 1) : base(RetEum.Success, errCode, message) { }
	//}

	///// <summary>
	///// 数组/集合下标越界异常
	///// </summary>
	//public class ApiArgumentIndexOutOfException : ApiException
	//{
	//	public ApiArgumentIndexOutOfException(string message = "下标越界错误", int errCode = 1) : base(RetEum.Success, errCode, message) { }
	//}
}
