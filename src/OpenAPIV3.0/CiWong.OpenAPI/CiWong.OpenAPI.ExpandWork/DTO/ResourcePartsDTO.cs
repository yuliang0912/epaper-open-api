using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class ResourcePartDTO
	{
		/// <summary>
		/// 子资源版本ID
		/// </summary>
		public long VersionId { get; set; }

		/// <summary>
		/// 子资源类型ID
		/// </summary>
		public string ResourceType { get; set; }
	}
}
