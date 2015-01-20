using Newtonsoft.Json;

namespace CiWong.OpenAPI.Core
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ApiResult<T> : ApiResult
	{
		[JsonProperty(PropertyName = "data")]
		public T Data { get; set; }
	}


	[JsonObject(MemberSerialization.OptIn)]
	public class ApiResult
	{
		/// <summary>
		/// 服务器状态码
		/// </summary>
		[JsonProperty(PropertyName = "ret")]
		public RetEum Ret { get; set; }

		/// <summary>
		/// 接口内部状态码
		/// </summary>
		[JsonProperty(PropertyName = "errcode")]
		public int ErrorCode { get; set; }

		/// <summary>
		/// 简单信息描述
		/// </summary>
		[JsonProperty(PropertyName = "msg")]
		public string Message { get; set; }
	}
}
