using CiWong.Agent.ApiCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CiWong.OpenAPI.Core;
using CiWong.Agent.ApiCore.Entities;


namespace CiWong.OpenAPI.Store.Controllers
{
	public class StoreController : ApiController
	{
		/// <summary>
		/// 获取校园书店的电子报
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic newspaper_books(long schoolId, int type, int productId = 0, int page = 1, int pageSize = 10)
		{
			if (type != 1)
			{
				throw new ApiArgumentException("type不在指定的范围之内");
			}

			int totalItem = 0, pageCount = 0;

			var list = PushProductProxy.GetPutawayProductList(page, pageSize, schoolId, out totalItem, out pageCount, productId, 380448713, 3);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Select(t => new
				{
					productId = t.ProductId.ToString(),
					productName = t.ProductName ?? string.Empty,
					cover = t.CoverImgUrl ?? string.Empty,
					packageId = t.PackageId,
					appId = 200003
				})
			};
		}
	}
}
