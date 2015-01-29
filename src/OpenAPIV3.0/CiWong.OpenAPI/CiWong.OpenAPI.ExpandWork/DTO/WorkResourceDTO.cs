using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ExpandWork.DTO
{
	public class WorkResourceDTO
	{
		public WorkResourceDTO()
		{
			this.resourceParts = Enumerable.Empty<ResourcePartDTO>();
		}

		/// <summary>
		/// 资源目录层级ID
		/// </summary>
		public string TaskId { get; set; }

		/// <summary>
		/// 目录模块ID
		/// </summary>
		public int ModuleId { get; set; }

		/// <summary>
		/// 资源版本ID
		/// </summary>
		public long VersionId { get; set; }

		/// <summary>
		/// 资源关联路径格式为:根版本ID/父版本ID/当前版本ID
		/// </summary>
		public string RelationPath { get; set; }

		/// <summary>
		/// 子模块ID(适用于子集没有版本ID的资源,例如跟读中的单词组)
		/// </summary>
		public string SonModuleId { get; set; }

		/// <summary>
		/// 资源名称
		/// </summary>
		public string ResourceName { get; set; }

		/// <summary>
		/// 基础资源类型ID
		/// </summary>
		public string ResourceType { get; set; }

		/// <summary>
		/// 作业要求
		/// </summary>
		public string RequirementContent { get; set; }

		/// <summary>
		/// 子资源集合
		/// </summary>
		public IEnumerable<ResourcePartDTO> resourceParts { get; set; }
	}
}
