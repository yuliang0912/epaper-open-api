using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class WorkAnswerDTO<T>
	{
		public WorkAnswerDTO()
		{
			this.Answers = Enumerable.Empty<T>();
		}

		/// <summary>
		/// 资源版本ID
		/// </summary>
		public long VersionId { get; set; }

		/// <summary>
		/// 答案类型(1:作业 2:自主练习)
		/// </summary>
		public int AnswerType { get; set; }

		/// <summary>
		/// 资源类型
		/// </summary>
		public string ResourceType { get; set; }

		/// <summary>
		/// 得分
		/// </summary>
		public decimal Score { get; set; }

		/// <summary>
		/// 1.正确 2.错误 3.半对 4:未知
		/// </summary>
		public int Assess { get; set; }

		/// <summary>
		/// 具体作业答案
		/// </summary>
		public IEnumerable<T> Answers { get; set; }
	}
}
