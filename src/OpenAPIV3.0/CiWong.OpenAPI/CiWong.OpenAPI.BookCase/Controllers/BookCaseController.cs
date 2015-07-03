using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.DataContracts;
using CiWong.Resource.BookRoom.Service;
using CiWong.Tools.Package.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.BookCase.Controllers
{
	public class BookCaseController : ApiController
	{
		private PackageService packageService;
		private ProductInfoService productInfoService;
		private UserproductService userproductService;
		private PackagePermissionService packagePermissionService;

		public BookCaseController(ProductInfoService _productInfoService, UserproductService _userproductService, PackagePermissionService _packagePermissionService, PackageService _packageService)
		{
			this.packageService = _packageService;
			this.productInfoService = _productInfoService;
			this.userproductService = _userproductService;
			this.packagePermissionService = _packagePermissionService;
		}

		/// <summary>
		/// 验证资源包中的资源使用权限(1:购买书籍 2:无权使用 3:已买书籍但过期 4:免费资源 5:开通服务 6:开通服务但过期)
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="versionId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public int is_can(long packageId, long versionId = 0)
		{
			if (DateTime.Now < new DateTime(2015, 9, 1))
			{
				return 4;
			}

			//是否资源被设置成免费
			if (versionId > 0)
			{
				var resource = packageService.GetTaskResultContentsForApi(packageId, versionId);

				if (null != resource && resource.Any(t => t.IsFree))
				{
					return 4;
				}
			}
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			//是否购买过书籍
			var packagePermission = packagePermissionService.GetEntity(packageId, userId);
			if (null != packagePermission && packagePermission.ExpirationDate > DateTime.Now)
			{
				return 1;
			}

			//是否购买过服务
			var lastOpenService = AppServiceProxy.GetOpenServiceList(userId, packageId).OrderByDescending(t => Convert.ToDateTime(t.ExpireTime)).FirstOrDefault();
			if (lastOpenService != null && !lastOpenService.bExpired)
			{
				return 5;
			}

			//已购买书籍,但是过期
			if (null != packagePermission)
			{
				return 3;
			}

			//已开通服务,但是过期
			if (null != lastOpenService)
			{
				return 6;
			}

			//不可用
			return 2;
		}

		/// <summary>
		/// 获取我的书柜中的书籍
		/// </summary>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic my_books(int productType = -1, int actionType = -1, int gradeId = -1, int subjectId = -1, int isPublish = -1, int page = 1, int pageSize = 20)
		{
			int totalItem = 0;

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var myBooks = productInfoService.GetMyListForApi(userId, productType, actionType, ref totalItem, gradeId, subjectId, page - 1, pageSize, isPublish);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = myBooks.Select(t => new
				{
					appId = t.AppId,
					productId = t.ProductId,
					packageId = t.PackageId,
					productName = t.ProductName,
					packageType = t.Type,
					cover = t.Cover,
					isPublish = t.IsDisplay == 1
				})
			};
		}

		/// <summary>
		/// 添加书籍到书柜
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="productId"></param>
		/// <param name="packageId"></param>
		/// <param name="addType">添加类型(2:自主添加 4:老师推荐 8:代理商推荐)</param>
		/// <param name="isPublish">是否在电子作业中显示</param>
		/// <returns></returns>
		[HttpGet, HttpPost, BasicAuthentication]
		public dynamic add_book(int appId, long productId, long packageId, int isPublish = 0)
		{
			var package = packageService.GetPackageForApi(packageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "未找到指定的资源包");
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new CiWong.Users.UserManager().GetUserInfo(userId);

			if (null == userInfo)
			{
				return new ApiArgumentException(ErrorCodeEum.BookCase_4901, "未找到指定的用户");
			}

			#region 参数实体构建
			var productInfo = new ProductInfoContract();
			productInfo.ProductId = productId.ToString();
			productInfo.AppId = appId;
			productInfo.AppName = appId == 200002 ? "6v68教育商城" : appId == 200003 ? "校园书店" : "未知";
			productInfo.PackageId = packageId;
			productInfo.ProductName = package.BookName;
			productInfo.Type = package.GroupType;
			productInfo.Author = package.TeamName;
			productInfo.Cover = package.Cover;
			productInfo.Summary = package.Introduction;
			productInfo.Price = Convert.ToDecimal(package.Price);
			productInfo.CreateDate = package.CreateTime;
			productInfo.AreaType = package.AreaType;
			productInfo.ProvinceId = package.ProvinceId;
			productInfo.ProvinceName = package.ProvincelName ?? string.Empty;
			productInfo.CityId = package.CityId;
			productInfo.CityName = package.CityName ?? string.Empty;
			productInfo.PeriodId = package.PeriodId;
			productInfo.GradeId = package.GradeId;
			productInfo.SubjectId = package.SubjectId;
			productInfo.SemestreId = package.SemesterId;
			productInfo.VersionId = package.BookVersion;
			productInfo.TeamId = (int)package.TeamId;
			productInfo.TeamName = package.TeamName;
			productInfo.CreateUserId = package.CreateUserId;
			productInfo.CreateUserName = package.CreateUserName;
			productInfo.IsDisplay = isPublish == 1 ? 1 : 0;

			var userProduct = new UserproductContract();
			userProduct.ProductId = productId.ToString();
			userProduct.AppId = appId;
			userProduct.PackageId = packageId;
			userProduct.UserId = userId;
			userProduct.UserName = userInfo.RealName;
			userProduct.AddType = 2;
			userProduct.AddUserId = userId;
			userProduct.AddUserName = userInfo.RealName;
			userProduct.CreateDate = DateTime.Now;
			userProduct.LastUpdateDate = DateTime.Now;
			#endregion

			var result = productInfoService.Add(new List<ProductInfoContract>() { productInfo }, new List<UserproductContract>() { userProduct }, DateTime.Now, productInfo.IsDisplay);

			if (!result.IsSucceed)
			{
				return new ApplicationException(result.Message);
			}

			return result.IsSucceed;
		}

		/// <summary>
		/// 设置书籍是否在电子作业中显示
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="productId"></param>
		/// <param name="isPublish"></param>
		/// <returns></returns>
		[HttpGet, HttpPost, BasicAuthentication]
		public dynamic set_book_to_work(int appId, long productId, int isPublish = 1)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			if (!productInfoService.ExistsProduct(appId, productId.ToString(), userId))
			{
				return new ApiArgumentException(ErrorCodeEum.BookCase_4902, "书柜中不存在指定的书籍");
			}

			var productInfo = productInfoService.GetEntity(appId, productId.ToString());

			if (null == productInfo)
			{
				return new ApiArgumentException(ErrorCodeEum.BookCase_4903, "未找到指定的书籍");
			}

			var package = packageService.GetPackageForApi(productInfo.PackageId);

			if (package.AreaType == -1)
			{
				return new ApiArgumentException(ErrorCodeEum.BookCase_4904, "当前书籍不允许添加到电子作业");
			}

			return userproductService.SetIsDisplay(userId, appId, productId.ToString(), isPublish == 1 ? 1 : 0);
		}
	}
}
