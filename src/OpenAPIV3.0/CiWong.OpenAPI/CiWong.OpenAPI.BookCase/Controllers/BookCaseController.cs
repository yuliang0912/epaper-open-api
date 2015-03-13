using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.Repository;
using CiWong.Tools.Package.Services;
using System;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.BookCase.Controllers
{
	public class BookCaseController : ApiController
	{
		/// <summary>
		/// 验证资源包中的资源使用权限(1:有 2:无 )
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="versionId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public int is_can(long packageId, long versionId)
		{
			var resource = new PackageService().GetTaskResultContentsForApi(packageId, versionId);

			if (null == resource || !resource.Any())
			{
				throw new ApiArgumentException("参数versionId错误，未找到指定资源");
			}

			if (resource.Any(t => t.IsFree))
			{
				return 4;
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var packagePermission = new PackagePermissionRepository().GetEntity(packageId, userId);

			if (null == packagePermission)
			{
				return 2;
			}
			else if (packagePermission.ExpirationDate > DateTime.Now)
			{
				return 1;
			}
			else
			{
				return 3;
			}
		}

		/// <summary>
		/// 获取我的书柜中的书籍
		/// </summary>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic my_books(int productType = -1, int gradeId = -1, int subjectId = -1, int page = 1, int pageSize = 20)
		{
			int totalItem = 0;
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var myBooks = new CiWong.Resource.BookRoom.Repository.ProductInfoRepository().GetMyListForApi(userId, productType, ref totalItem, gradeId, subjectId, page - 1, pageSize);

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
