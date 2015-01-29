using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class WorkPackageDTO
	{
		public WorkPackageDTO()
		{
			this.workResources = Enumerable.Empty<WorkResourceDTO>();
		}

		/// <summary>
		/// 所属产品ID
		/// </summary>
		public long ProductId { get; set; }

		/// <summary>
		/// 产品所属平台ID
		/// </summary>
		public int AppId { get; set; }

		/// <summary>
		/// 跟读资源包ID
		/// </summary>
		public long PackageId { get; set; }

		/// <summary>
		/// 资源名称
		/// </summary>
		public string PackageName { get; set; }

		/// <summary>
		/// 产品类型(1电子书,2课程,3电子报)
		/// </summary>
		public int PackageType { get; set; }

		/// <summary>
		/// 作业资源集合
		/// </summary>
		public IEnumerable<WorkResourceDTO> workResources { get; set; }
	}
}
