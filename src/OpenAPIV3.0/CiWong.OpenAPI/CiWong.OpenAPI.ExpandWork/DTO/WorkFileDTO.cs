
namespace CiWong.OpenAPI.ExpandWork.DTO
{
	/// <summary>
	/// 作业附件
	/// </summary>
	public class WorkFileDTO
	{
		/// <summary>
		/// 附件名称
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// 附件地址
		/// </summary>
		public string FileUrl { get; set; }

		/// <summary>
		/// 附件格式(.png .mp4等)
		/// </summary>
		public string FileExt { get; set; }

		/// <summary>
		/// 文件类型(图片=1,音频=2,视频=3,Word文档=4)
		/// </summary>
		public int FileType { get; set; }

		/// <summary>
		/// 文件描述
		/// </summary>
		public string FileDesc { get; set; }
	}
}
