using CiWong.OpenAPI.Core;
using CiWong.YiShang.YiShang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CiWong.OpenAPI.YiShang.Controllers
{
	public class YiShangController : ApiController
	{
		private ProductRepository _productRepository;
		public YiShangController(ProductRepository _productRepository)
		{
			this._productRepository = _productRepository;
		}

		/// <summary>
		/// 6v68书城获取同步跟读和模考书籍
		/// </summary>
		/// <param name="type">电子书类型 1:同步跟读(乐享英语店铺)  2:听说模考(学子驿站店铺)</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic ebooks(int type, long productId = 0, int page = 1, int pageSize = 10)
		{
			int tearmId = 0, categorytype = 0;
			if (type == 1)
			{
				tearmId = 459986237;
				categorytype = 3;
			}
			else if (type == 2)
			{
				tearmId = 459986237;
				categorytype = 2370;
			}
			else
			{
				throw new ApiArgumentException("type不在指定的范围之内");
			}
			var list = _productRepository.GetProductListToBookCase(productId, 1001, 1, page - 1, pageSize, true, tearmId, categorytype);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = list.TotalCount,
				PageList = list.Entity.Select(t => new
				{
					productId = t.ProductId,
					productName = t.ProductName ?? string.Empty,
					cover = t.Cover ?? string.Empty,
					packageId = t.PackageId,
					appId = 200002
				})
			};
		}
	}
}
