using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.Resource.BookRoom.Service;
using CiWong.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.Agent.Controllers
{
	/// <summary>
	/// 代理商后台
	/// </summary>
	public class AgentController : ApiController
	{
		private ProductInfoService productInfoService;
		public AgentController(ProductInfoService _productInfoService)
		{
			this.productInfoService = _productInfoService;
		}

		/// <summary>
		/// 为班级申请试用服务
		/// </summary>
		/// <param name="classIds"></param>
		/// <param name="serviceType">服务类型,阳光英语:25</param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic apply_service(long classId, int serviceType)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new UserManager().GetUserInfo(userId);

			var result = AppServiceProxy.ApplyUseService(userId, userInfo.RealName, classId, serviceType);

			return new ApiResult()
			{
				Ret = (RetEum)result.ret,
				ErrorCode = (ErrorCodeEum)result.errcode,
				Message = result.msg
			};
		}

		/// <summary>
		/// 获取班级申请情况记录
		/// </summary>
		/// <param name="classIds"></param>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic class_apply_record(string classIds, int serviceType)
		{
			var classList = classIds.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Select(t => Convert.ToInt64(t)).ToList();

			return AppServiceProxy.GetOpenServiceList(classList, serviceType).Select(t => new
			{
				classId = t.ClassId,
				className = t.ClassName,
				serviceType = t.ServiceType,
				useDayNum = t.UseDayNum,
				expireTime = t.ExpireTime,
				isDelay = t.IsDelay,
				isTimeOut = t.ExpireTime < DateTime.Now
			});
		}

		/// <summary>
		/// 获取用户是否拥有资源包的使用服务权限
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic open_service_list(long packageId, int userId)
		{
			return AppServiceProxy.GetOpenServiceList(userId, packageId).Select(t => new
			{
				userId = t.UseId,
				userName = t.UseName,
				serviceType = t.ServiceType,
				serviceTypeName = t.ServiceTypeName,
				price = t.Price,
				expireTime = t.ExpireTime,
				isTimeOut = Convert.ToDateTime(t.ExpireTime) < DateTime.Now
			});
		}


		/// <summary>
		/// 书籍所属服务列表
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic service_list(long packageId)
		{
			var priceList = new List<Tuple<decimal, string, int, decimal>>();
			priceList.Add(new Tuple<decimal, string, int, decimal>(200m, "半年", 6, 10m));
			priceList.Add(new Tuple<decimal, string, int, decimal>(360m, "一年", 12, 9m));
			priceList.Add(new Tuple<decimal, string, int, decimal>(720m, "三年", 36, 6m));

			return AppServiceProxy.GetServiceList(packageId).Select(t => new
			{
				serviceId = t.ID,
				serviceName = t.Name,
				priceList = priceList.Select(x => new
				{
					price = x.Item1,
					unitName = x.Item2,
					months = x.Item3,
					discount = x.Item4
				})
			});
		}

		/// <summary>
		/// 获取服务书籍
		/// </summary>
		/// <param name="serviceId">25:阳光英语电子报 26:乐享英语</param>
		/// <param name="schoolId">学校ID</param>
		/// <param name="bookType">书籍分类,1教材同步 2课外拓展 -1为所有</param>
		/// <param name="keyWords">关键字搜索</param>
		/// <returns>阳光英语电子报服务书籍库(省市ID同时为0,则查询全国)</returns>
		[HttpGet]
		public dynamic service_products(int serviceId, long schoolId = 0, int bookType = -1, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int provId = -1, cityId = -1, totalItem = 0;

			var school = CiWong.Relation.WCFProxy.ClassRelationProxy.GetRoomSchool(schoolId);

			if (null != school && !string.IsNullOrEmpty(school.SchoolArea))
			{
				provId = school.SchoolArea.Length >= 2 ? Convert.ToInt32(school.SchoolArea.Substring(0, 2)) : -1;
				cityId = school.SchoolArea.Length >= 4 ? Convert.ToInt32(school.SchoolArea.Substring(0, 4)) : -1;
			}

			var list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, serviceId, bookType, provId, cityId, keyWords);

			if (totalItem == 0 && page == 1 && string.IsNullOrEmpty(keyWords))
			{
				list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, serviceId, bookType, -1, -1, keyWords);
			}

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
					grade = t.ProductGrade,
					bookIntro = t.Introduction.RemoveHtml().CutString(300)
				})
			};
		}


		/// <summary>
		/// 获取服务书籍
		/// </summary>
		/// <param name="serviceId">25:阳光英语电子报 26:乐享英语</param>
		/// <param name="bookType">书籍分类,1教材同步 2课外拓展 -1为所有</param>
		/// <param name="keyWords">关键字搜索</param>
		/// <returns>阳光英语电子报服务书籍库(省市ID同时为0,则查询全国)</returns>
		[BasicAuthentication, HttpGet]
		public dynamic my_service_products(int serviceId, int bookType = -1, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int provId = -1, cityId = -1, totalItem = 0;

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var school = CiWong.Relation.WCFProxy.ClassRelationProxy.GetRoomSchoolByUserList(new List<int>() { userId }).FirstOrDefault();

			if (null != school && !string.IsNullOrEmpty(school.SchoolArea))
			{
				provId = school.SchoolArea.Length >= 2 ? Convert.ToInt32(school.SchoolArea.Substring(0, 2)) : -1;
				cityId = school.SchoolArea.Length >= 4 ? Convert.ToInt32(school.SchoolArea.Substring(0, 4)) : -1;
			}

			var list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, serviceId, bookType, provId, cityId, keyWords);

			if (totalItem == 0 && page == 1 && string.IsNullOrEmpty(keyWords))
			{
				list = PushProductProxy.GetApplicationServiceList(out totalItem, page, pageSize, serviceId, bookType, -1, -1, keyWords);
			}

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
					grade = t.ProductGrade,
					bookIntro = t.Introduction.RemoveHtml().CutString(300)
				})
			};
		}

		/// <summary>
		/// 习网大书库书柜书籍列表
		/// </summary>
		/// <param name="subjectId"></param>
		/// <param name="periodId"></param>
		/// <param name="gradeId"></param>
		/// <param name="keyWords"></param>
		/// <param name="page"></param>
		/// <param name="pageSize"></param>
		/// <returns></returns>
		[HttpGet, HttpPost, BasicAuthentication]
		public dynamic list_ciwong_shelf_book(int subjectId = 0, int periodId = 0, int gradeId = 0, string keyWords = "", int page = 1, int pageSize = 10)
		{
			int totalItem = 0;

			var productList = PushProductProxy.GetCiWongBookList(ref totalItem, periodId, gradeId, subjectId, keyWords, page, pageSize);

			var shelfBooks = new Dictionary<long, int>();

			if (productList.Any())
			{
				var userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
				var productIds = string.Join(",", productList.Select(t => t.ProductId));
				shelfBooks = productInfoService.GetExistsProductList(200002, userId, productIds).ToDictionary(c => Convert.ToInt64(c.ProductId), c => c.IsDisplay);
			}

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = productList.Select(t => new
				{
					appId = t.ProductId == t.PackageId ? 200003 : 200002,
					productId = t.ProductId.ToString(),
					productName = t.ProductName ?? string.Empty,
					cover = t.CoverImgUrl ?? string.Empty,
					packageId = t.PackageId,
					packageType = t.ProductType,
					isPublish = shelfBooks.ContainsKey(t.ProductId) ? shelfBooks[t.ProductId] == 1 : false,
					isAddShelf = shelfBooks.ContainsKey(t.ProductId),
					teamId = t.TeamId,
					teamName = t.TeamName,
					period = t.ProductPeriod,
					grade = t.ProductGrade
				})
			};
		}

		/// <summary>
		/// 用户自定义设置品牌
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public bool set_subscribe_service(int serviceType)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			return AppServiceProxy.ServiceTypeUserRelation(userId, serviceType);
		}

		/// <summary>
		/// 获取自己选择的服务品牌
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public int get_subscribe_service()
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			return AppServiceProxy.GetUserServiceType(userId);
		}
	}
}
