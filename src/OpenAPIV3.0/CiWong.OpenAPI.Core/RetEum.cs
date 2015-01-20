
namespace CiWong.OpenAPI.Core
{
	public enum RetEum
	{
		/// <summary>
		/// 未定义错误
		/// </summary>
		UndefinedError = -2,
		/// <summary>
		/// HTTP请求错误
		/// </summary>
		HttpError = -1,
		/// <summary>
		/// 正常返回
		/// </summary>
		Success = 0,
		/// <summary>
		/// 参数错误
		/// </summary>
		ArgumentError = 1,
		/// <summary>
		/// 频率受限
		/// </summary>
		FrequencyLimit = 2,
		/// <summary>
		/// 鉴权失败
		/// </summary>
		AuthenticationFailure = 3,
		/// <summary>
		/// 服务器内部错误
		/// </summary>
		ServerError = 4,
		/// <summary>
		/// 应用程序错误
		/// </summary>
		ApplicationError = 5
	}
}
