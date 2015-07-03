using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.Service;
using CiWong.Tools.Package.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.Store.Controllers
{
	public class StoreController : ApiController
	{
		private PackageService packageService;
		private ProductInfoService productInfoService;
		public StoreController(PackageService _packageService, ProductInfoService _productInfoService)
		{
			this.packageService = _packageService;
			this.productInfoService = _productInfoService;
		}

		/// <summary>
		/// 获取校园书店的电子报
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic newspaper_books(long schoolId, int type, int productId = 0, int page = 1, int pageSize = 10)
		{
			int totalItem = 0, pageCount = 0;

			var list = PushProductProxy.GetPutawayProductList(page, pageSize, schoolId, out totalItem, out pageCount, productId, 380448713, 3);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Select(t => new
				{
					appId = t.ProductId == t.PackageId ? 200003 : 200002,
					productId = t.ProductId.ToString(),
					productName = t.ProductName ?? string.Empty,
					cover = t.CoverImgUrl ?? string.Empty,
					packageId = t.PackageId,
					packageType = t.ProductType
				})
			};
		}

		/// <summary>
		/// 获取阳光英语服务书籍[已取消的接口]
		/// </summary>
		/// <param name="serviceId">25:阳光英语电子报 26:乐享英语</param>
		/// <param name="provId">省ID,-1为所有</param>
		/// <param name="cityId">市ID,-1为所有</param>
		/// <param name="bookType">书籍分类,1教材同步 2课外拓展 -1为所有</param>
		/// <param name="keyWords">关键字搜索</param>
		/// <returns>阳光英语电子报服务书籍库(省市ID同时为0,则查询全国)</returns>
		[HttpGet]
		public dynamic sunshine_service_products(int provId = -1, int cityId = -1, int bookType = -1, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int totalItem = 0;

			var list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, 25, bookType, provId, cityId, keyWords);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Select(t => new
				{
					appId = t.ProductId == t.PackageId ? 200003 : 200002,
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
		/// 获取服务书籍
		/// </summary>
		/// <param name="serviceId">25:阳光英语电子报 26:乐享英语</param>
		/// <param name="provId">省ID,-1为所有</param>
		/// <param name="cityId">市ID,-1为所有</param>
		/// <param name="bookType">书籍分类,1教材同步 2课外拓展 -1为所有</param>
		/// <param name="keyWords">关键字搜索</param>
		/// <returns>阳光英语电子报服务书籍库(省市ID同时为0,则查询全国)</returns>
		[HttpGet]
		public dynamic service_products(int serviceType, int provId = -1, int cityId = -1, int bookType = -1, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int totalItem = 0;

			var list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, serviceType, bookType, provId, cityId, keyWords);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Select(t => new
				{
					appId = t.ProductId == t.PackageId ? 200003 : 200002,
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
				appId = product.ProductId == product.PackageId ? 200003 : 200002,
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

		/// <summary>
		/// 基础产品信息
		/// </summary>
		/// <param name="packageId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic product_info(long packageId)
		{
			var package = packageService.GetPackageForApi(packageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "未找到指定的资源包");
			}

			return new
			{
				appId = 200003,
				productId = package.PackageId.ToString(),
				productName = package.BookName ?? string.Empty,
				cover = package.Cover ?? string.Empty,
				packageId = package.PackageId,
				packageType = package.GroupType,
			};
		}

		/// <summary>
		/// 获取校园书店中的书籍
		/// </summary>
		/// <param name="schoolId">学校ID</param>
		/// <param name="gradeId">年级ID</param>
		/// <param name="subjectId">科目ID</param>
		/// <param name="page"></param>
		/// <param name="pageSize"></param>
		/// <returns></returns>
		[HttpGet, HttpPost, BasicAuthentication]
		public dynamic list_product(long schoolId, int gradeId = 0, int subjectId = 0, int page = 1, int pageSize = 10)
		{
			var productResult = PushProductProxy.GetProductListBySchoolId(schoolId, gradeId: gradeId, subjectId: subjectId, pageIndex: page, pageSize: pageSize);

			var shelfBooks = new Dictionary<long, int>();

			if (productResult.DataList.Any())
			{
				var userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
				var productIds = string.Join(",", productResult.DataList.Select(t => t.ProductId));
				shelfBooks = productInfoService.GetExistsProductList(200003, userId, productIds).ToDictionary(c => Convert.ToInt64(c.ProductId), c => c.IsDisplay);
			}

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = (int)productResult.RecordCount,
				PageList = productResult.DataList.Select(t => new
				{
					appId = t.ProductId == t.PackageId ? 200003 : 200002,
					productId = t.ProductId.ToString(),
					productName = t.ProductName ?? string.Empty,
					cover = t.CoverImgUrl ?? string.Empty,
					packageId = t.PackageId,
					packageType = t.ProductType,
					isPublish = shelfBooks.ContainsKey(t.ProductId) ? shelfBooks[t.ProductId] == 1 : false,
					isAddShelf = shelfBooks.ContainsKey(t.ProductId),
					price = t.Price
				})
			};
		}

		/// <summary>
		/// 是否开通校园书店
		/// </summary>
		/// <param name="schoolId"></param>
		/// <returns></returns>
		[HttpGet, HttpPost]
		public dynamic is_open_school_store(long schoolId)
		{
			return PushProductProxy.SchoolExist(schoolId);
		}
	}
}		 


