using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.Repository;
using CiWong.Resource.BookRoom.Service;
using CiWong.Tools.Package.Services;
using System;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.BookCase.Controllers
{
	public class BookCaseController : ApiController
	{
		private ProductInfoService productInfoRepository;
		public BookCaseController(ProductInfoService _productInfoRepository)
		{
			this.productInfoRepository = _productInfoRepository;
		}

		/// <summary>
		/// 验证资源包中的资源使用权限(1:有 2:无 )
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="versionId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public int is_can(long packageId, long versionId = 0)
		{
			//是否资源被设置成免费
			if (versionId > 0)
			{
				var resource = new PackageService().GetTaskResultContentsForApi(packageId, versionId);

				if (null != resource && resource.Any(t => t.IsFree))
				{
					return 4;
				}
			}
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			//是否购买过书籍
			var packagePermission = new PackagePermissionRepository().GetEntity(packageId, userId);
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
		public dynamic my_books(int productType = -1, int actionType = -1, int gradeId = -1, int subjectId = -1, int page = 1, int pageSize = 20)
		{
			int totalItem = 0;
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var myBooks = productInfoRepository.GetMyListForApi(userId, productType, actionType, ref totalItem, gradeId, subjectId, page - 1, pageSize);

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
					cover = t.Cover
				})
			};
		}
	}
}
