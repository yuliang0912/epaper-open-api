using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class UnitWorkDTO<T>
	{
		public UnitWorkDTO()
		{
			this.WorkAnswers = Enumerable.Empty<WorkAnswerDTO<T>>();
		}

		/// <summary>
		/// 做作业ID(作业系统)
		/// </summary>
		public long DoWorkId { get; set; }

		/// <summary>
		/// 布置资源内容ID
		/// </summary>
		public long ContentId { get; set; }

		/// <summary>
		/// /作业时长
		/// </summary>
		public int WorkLong { get; set; }

		/// <summary>
		/// 作业答案
		/// </summary>
		public IEnumerable<WorkAnswerDTO<T>> WorkAnswers { get; set; }
	}
}
