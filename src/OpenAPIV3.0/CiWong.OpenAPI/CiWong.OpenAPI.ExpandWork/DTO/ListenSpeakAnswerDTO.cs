using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class ListenSpeakAnswerDTO
	{
		/// <summary>
		/// 选择题选项ID
		/// </summary>
		public string OptionId { get; set; }

		/// <summary>
		/// 录音文件URL 情景对话录音文件
		/// </summary>
		public string AudioUrl { get; set; }

		/// <summary>
		/// 填空题答案
		/// </summary>
		public string BlankContent { get; set; }
	}
}
