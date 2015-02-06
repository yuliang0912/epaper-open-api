using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	/// <summary>
	/// 涂鸦附件作业
	/// </summary>
	public class GraffitiFileWorkDTO
	{
		public GraffitiFileWorkDTO()
		{
			this.GraffitiFiles = Enumerable.Empty<GraffitiFileDTO>();
		}

		public long DoId { get; set; }

		public long DoWorkId { get; set; }

		public IEnumerable<GraffitiFileDTO> GraffitiFiles { get; set; }
	}

	/// <summary>
	/// 涂鸦图片信息
	/// </summary>
	public class GraffitiFileDTO
	{
		public int Sid { get; set; }

		public string Comment { get; set; }
	}
}
