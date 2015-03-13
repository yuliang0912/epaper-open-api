using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using System.Linq;
using System.Web.Http;

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
				return new ApiArgumentException("type不在指定的范围之内");
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
					packageType = t.ProductType,
					appId = 200003
				})
			};
		}

		/// <summary>
		/// 获取阳光英语服务书籍
		/// </summary>
		/// <param name="provId">省ID,-1为所有</param>
		/// <param name="cityId">市ID,-1为所有</param>
		/// <param name="bookType">书籍分类,1教材同步 2课外拓展 -1为所有</param>
		/// <param name="keyWords">关键字搜索</param>
		/// <returns>阳光英语电子报服务书籍库(省市ID同时为0,则查询全国)</returns>
		[HttpGet]
		public dynamic sunshine_service_products(int provId = -1, int cityId = -1, int bookType = -1, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int totalItem = 0;

			//阳光英语服务类型ID:25
			var list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, 25, bookType, provId, cityId, keyWords);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Select(t => new
				{
					appId = 200003,
					productId = t.ProductId.ToString(),
					productName = t.ProductName ?? string.Empty,
					cover = t.CoverImgUrl ?? string.Empty,
					packageId = t.PackageId,
					packageType = t.ProductType,
					bookType = t.BookType, //书籍分类,1教材同步 2课外拓展
					teamId = t.TeamId,
					teamName = t.TeamName,
					provId = t.ProvId,
					provName = t.ProvName,//省级
					cityId = t.CityId,
					cityName = t.CityName,//市级
					period = t.ProductPeriod,
					grade = t.ProductGrade
				})
			};
		}

		/// <summary>
		/// 获取校园书店服务频道书籍详情
		/// </summary>
		/// <param name="packageId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic service_product_detail(long packageId)
		{
			var product = PushProductProxy.GetApplicationService(packageId);

			return new
			{
				appId = 200003,
				productId = product.ProductId.ToString(),
				productName = product.ProductName ?? string.Empty,
				cover = product.CoverImgUrl ?? string.Empty,
				packageId = product.PackageId,
				packageType = product.ProductType,
				bookType = product.BookType, //书籍分类,1教材同步 2课外拓展
				teamId = product.TeamId,
				teamName = product.TeamName,
				provId = product.ProvId,
				provName = product.ProvName,//省级
				cityId = product.CityId,
				cityName = product.CityName,//市级
				period = product.ProductPeriod,
				grade = product.ProductGrade
			};
		}
	}
}		 


