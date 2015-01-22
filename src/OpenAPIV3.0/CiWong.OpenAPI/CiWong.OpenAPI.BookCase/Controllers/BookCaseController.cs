using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CiWong.OpenAPI.BookCase.Controllers
{
	[BasicAuthentication]
	public class BookCaseController : ApiController
	{
		private PackagePermissionService packagePermissionService;

		public BookCaseController(PackagePermissionService packagePermissionService)
		{
			this.packagePermissionService = packagePermissionService;
		}

		[HttpGet]
		public int is_can(long packageId, long versionId)
		{

			return 1;

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var packagePermission = packagePermissionService.GetEntity(packageId, userId);
		}
	}
}
