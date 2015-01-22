using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CiWong.OpenAPI.Core
{
	[DataContract(Name = "pageList")]
	public class ApiPageList<T>
	{
		public ApiPageList()
		{
			this.PageList = Enumerable.Empty<T>();
		}

		/// <summary>
		/// 页码(从1开始)
		/// </summary>
		[DataMember(Name = "page")]
		public int Page { get; set; }

		/// <summary>
		/// 每页数量
		/// </summary>
		[DataMember(Name = "pageSize")]
		public int PageSize { get; set; }

		/// <summary>
		/// 总数据量
		/// </summary>
		[DataMember(Name = "totalCount")]
		public int TotalCount { get; set; }

		/// <summary>
		/// 总页数
		/// </summary>
		[DataMember(Name = "pageCount")]
		public int PageCount
		{
			get
			{
				if (TotalCount < 1 || PageSize < 1)
				{
					return 0;
				}
				return (int)Math.Ceiling(TotalCount * 1.0 / PageSize);
			}
		}


		[DataMember(Name = "pageList")]
		public IEnumerable<T> PageList { get; set; }
	}
}
