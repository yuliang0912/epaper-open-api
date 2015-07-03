
namespace CiWong.OpenAPI.Core
{
	/// <summary>
	/// 一级错误码定义
	/// </summary>
	public enum RetEum
	{
		/// <summary>
		/// 正常返回
		/// </summary>
		Success = 0,
		/// <summary>
		/// 服务器内部错误
		/// </summary>
		ServerError = 1,
		/// <summary>
		/// 非法参数错误
		/// </summary>
		ArgumentError = 2,
		/// <summary>
		/// 鉴权失败
		/// </summary>
		AuthenticationFailure = 3
	}
}
