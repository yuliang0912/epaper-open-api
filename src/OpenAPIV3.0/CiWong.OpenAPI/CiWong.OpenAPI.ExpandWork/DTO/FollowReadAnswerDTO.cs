
namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class FollowReadAnswerDTO
	{
		/// <summary>
		/// 单词或者句子内容
		/// </summary>
		public string Word { get; set; }

		/// <summary>
		/// 录音文件URL
		/// </summary>
		public string AudioUrl { get; set; }

		/// <summary>
		/// 跟读次数
		/// </summary>
		public string ReadTimes { get; set; }
	}
}
