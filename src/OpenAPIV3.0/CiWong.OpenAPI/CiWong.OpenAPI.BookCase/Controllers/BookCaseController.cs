﻿using CiWong.OpenAPI.Core;
using CiWong.Resource.BookRoom.Repository;
using CiWong.Resource.BookRoom.Service;
using CiWong.Tools.Package;
using CiWong.Tools.Package.Services;
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
		[HttpGet]
		public int is_can(long packageId, long versionId)
		{
			var resource = new PackageService().GetTaskResultForApi(packageId, null, null)
							.SelectMany(t => t.ResultContents)
							.FirstOrDefault(t => t.ResourceVersionId == versionId);

			if (null == resource)
			{
				throw new ApiArgumentException("参数versionId错误，未找到指定资源");
			}

			if (resource.IsFree)
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
	}
}
