using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	/// <summary>
	/// 作业快传
	/// </summary>
	public class FileWorkDTO
	{
		/// <summary>
		/// 做作业ID(作业系统)
		/// </summary>
		public long DoWorkId { get; set; }

		/// <summary>
		/// 附件资源包ID
		/// </summary>
		public long RecordId { get; set; }

		/// <summary>
		/// /作业时长
		/// </summary>
		public int WorkLong { get; set; }

		/// <summary>
		/// 附加留言
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// 作业答案
		/// </summary>
		public IEnumerable<WorkFileDTO> WorkAnswers { get; set; }
	}
}
