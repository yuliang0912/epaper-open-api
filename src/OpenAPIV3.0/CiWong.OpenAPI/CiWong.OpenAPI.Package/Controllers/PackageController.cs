using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using CiWong.OpenAPI.Core;
using CiWong.Tools.Package;
using CiWong.Tools.Package.Services;
using CiWong.OpenAPI.ToolsAndPackage.Helper;
using CiWong.Examination.API;
using CiWong.OpenAPI.Core.Extensions;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class PackageController : ApiController
	{
		private IExaminationAPI examApi;
		private PackageService packageService;
		public static readonly List<int> newsPaperModuleSortArray = new List<int> { 7, 10, 15, 18, 9, 5, 8 };

		public PackageController(PackageService _packageService, IExaminationAPI _examApi)
		{
			this.examApi = _examApi;
			this.packageService = _packageService;
		}

		/// <summary>
		/// 获取资源包信息
		/// </summary>
		/// <param name="packageId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic package_info(long packageId)
		{
			var package = packageService.GetPackageForApi(packageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "未找到指定的资源包");
			}

			return new
			{
				packageId = package.PackageId,
				packageName = package.BookName,
				packageType = package.GroupType,
				cover = package.Cover,
				price = package.Price,
				subjectId = package.SubjectId,
				periodId = package.PeriodId,
				gradeId = package.GradeId,
				areaType = package.AreaType
			};
		}

		/// <summary>
		/// 获取书籍章节
		/// </summary>
		/// <param name="packageId">资源包ID,必选</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic catalogues(long packageId, string cid = null, bool isDesc = false)
		{
			var result = packageService.GetCataloguesForApi(packageId, true);

			var downLoadUrls = packageService.GetOfflinePackageInfo(packageId).ToDictionary(t => t.ResourceId, t => t.Url ?? string.Empty);

			if (!string.IsNullOrWhiteSpace(cid))
			{
				var resultFilter = result.Where(t => t.ID == cid).FirstOrDefault();

				if (null == resultFilter)
				{
					return Enumerable.Empty<object>();
				}

				resultFilter.Recursion(item =>
				{
					item.Children = result.Where(c => c.ParentId != null && c.ParentId.Equals(item.ID)).OrderBy(c => c.DisplayOrder);
				}, item => item.Children);


				return new List<object>
				{
					new	
					{
						id = resultFilter.ID ?? string.Empty,
						name = resultFilter.Name ?? string.Empty,
						downLoadUrl = downLoadUrls.ContainsKey(resultFilter.ID) ? downLoadUrls[resultFilter.ID] : string.Empty,
						children = resultFilter.Children.Select(t => ToolsHelper.CatalogueFunc(t,downLoadUrls))
					}
				};
			}
			else if (isDesc)
			{
				result = result.OrderByDescending(t => t.DisplayOrder).ToList();
			}

			var catalogueTree = result.Where(item => item.Level.Equals(1));
			foreach (var catalogue in catalogueTree)
			{
				catalogue.Recursion(item =>
				{
					item.Children = result.Where(c => c.ParentId != null && c.ParentId.Equals(item.ID)).OrderBy(c => c.DisplayOrder);
				}, item => item.Children);
			}

			return catalogueTree.Select(x => new
			{
				id = x.ID ?? string.Empty,
				name = x.Name ?? string.Empty,
				downLoadUrl = downLoadUrls.ContainsKey(x.ID) ? downLoadUrls[x.ID] : string.Empty,
				children = x.Children.Select(t => ToolsHelper.CatalogueFunc(t, downLoadUrls))
			});
		}

		/// <summary>
		/// 获取书籍章节内容
		/// </summary>
		/// <param name="packageId">资源包ID,必选</param>
		/// <param name="cId">目录ID(最末级目录id),必选</param>
		/// <param name="isFilter">目是否过滤同步讲练</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic book_resources(long packageId, string cId, string moduleIds = "", string versionIds = "", bool isFilter = true)
		{
			var packageCategoryContent = packageService.GetTaskResultContentsForApi(packageId, cId, false);

			if (null == packageCategoryContent || !packageCategoryContent.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5004, "当前目录中未找到资源");
			}

			var moduleIdList = new List<int>();
			var versionIdList = new List<long>();
			if (!string.IsNullOrWhiteSpace(moduleIds))
			{
				moduleIdList = moduleIds.Split(',').Select(t => Convert.ToInt32(t)).ToList();
			}
			if (!string.IsNullOrEmpty(versionIds))
			{
				versionIdList = versionIds.Split(',').Select(t => Convert.ToInt64(t)).ToList();
			}
			if (moduleIdList.Any())
			{
				packageCategoryContent = packageCategoryContent.Where(t => moduleIdList.Contains(t.ModuleId)).ToList();
			}
			if (versionIdList.Any())
			{
				packageCategoryContent = packageCategoryContent.Where(t => versionIdList.Contains(t.ResourceVersionId)).ToList();
			}
			if (isFilter)
			{
				packageCategoryContent = packageCategoryContent.Where(t => t.ModuleId != 9).ToList();
			}

			var taskResultContent = packageCategoryContent.
									OrderBy(t => newsPaperModuleSortArray.IndexOf(t.ModuleId)).
									GroupBy(t => t.ModuleId).
									ToDictionary(c => c.Key, c => c.OrderBy(x => x.DisplayOrder).ToList());

			var jsonData = taskResultContent.Select(t => new
			{
				moduleInfo = new
				{
					packageCatalogueId = cId ?? string.Empty,
					moduleId = t.Key,
					moduleName = t.Value.First().ModuleName ?? string.Empty
				},
				resourceList = ToolsHelper.ResourceList(t.Value, t.Key).Select(m => new
				{
					parentVersionId = m.Id != null ? m.Id.Value : 0,
					versionId = m.VersionId ?? 0,
					name = m.Name ?? string.Empty,
					resourceModuleId = m.ModuleId ?? Guid.Empty
				})
			});

			return jsonData;
		}


		///// <summary>
		///// 获取书籍章节内容
		///// </summary>
		///// <param name="packageId">资源包ID,必选</param>
		///// <param name="cId">目录ID(最末级目录id),必选</param>
		///// <param name="isFilter">目是否过滤同步讲练</param>
		///// <returns></returns>
		//[HttpGet]
		//public dynamic book_resources(long packageId, string cId, string moduleIds = "", string versionIds = "", bool isFilter = true)
		//{
		//	var taskResultCategories = packageService.GetTaskResultCategoriesForApi(packageId, cId);
		//	var taskResultContents = packageService.GetTaskResultContentsForApi(packageId, cId, false).OrderBy(t => t.DisplayOrder).ToList();

		//	if (!taskResultContents.Any() || !taskResultCategories.Any())
		//	{
		//		return Enumerable.Empty<object>();
		//		//return new ApiArgumentException(ErrorCodeEum.Resource_5004, "当前目录中未找到资源");
		//	}

		//	var moduleIdList = new List<int>();
		//	var versionIdList = new List<long>();
		//	if (!string.IsNullOrWhiteSpace(moduleIds))
		//	{
		//		moduleIdList = moduleIds.Split(',').Select(t => Convert.ToInt32(t)).ToList();
		//	}
		//	if (!string.IsNullOrEmpty(versionIds))
		//	{
		//		versionIdList = versionIds.Split(',').Select(t => Convert.ToInt64(t)).ToList();
		//	}
		//	if (moduleIdList.Any())
		//	{
		//		taskResultCategories = taskResultCategories.Where(t => moduleIdList.Contains(t.ModuleId)).ToList();
		//	}
		//	if (versionIdList.Any())
		//	{
		//		taskResultContents = taskResultContents.Where(t => versionIdList.Contains(t.ResourceVersionId)).ToList();
		//	}
		//	if (isFilter)
		//	{
		//		taskResultCategories = taskResultCategories.Where(t => t.ModuleId != 9).ToList();
		//	}

		//	var taskResultContentDict = taskResultContents.GroupBy(c => c.CatalogueId).ToDictionary(c => c.Key, c => c.ToList());

		//	var jsonData = taskResultCategories.Where(t => taskResultContentDict.ContainsKey(t.Id)).Select(t => new
		//	{
		//		moduleInfo = new
		//		{
		//			packageCatalogueId = cId ?? string.Empty,
		//			moduleId = t.ModuleId,
		//			moduleName = t.Name ?? string.Empty
		//		},
		//		resourceList = ToolsHelper.ResourceList(taskResultContentDict[t.Id], t.ModuleId).Select(m => new
		//		{
		//			parentVersionId = m.Id != null ? m.Id.Value : 0,
		//			versionId = m.VersionId ?? 0,
		//			name = m.Name ?? string.Empty,
		//			resourceModuleId = m.ModuleId ?? Guid.Empty
		//		})
		//	});

		//	return jsonData;
		//}


		/// <summary>
		/// 获取书籍章节内容V2
		/// </summary>
		/// <param name="packageId">资源包ID,必选</param>
		/// <param name="cId">目录ID(最末级目录id),必选</param>
		/// <param name="isFilter">目是否过滤同步讲练</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic book_resources_v2(long packageId, string cId)
		{
			var taskResultCategories = packageService.GetTaskResultCategoriesForApi(packageId, cId);
			var taskResultContents = packageService.GetTaskResultContentsForApi(packageId, cId, false).OrderBy(t => t.DisplayOrder).ToList();

			if (!taskResultContents.Any() || !taskResultCategories.Any())
			{
				return Enumerable.Empty<object>();
			}

			var taskResultContentDict = taskResultContents.GroupBy(c => c.CatalogueId).ToDictionary(c => c.Key, c => c.ToList());

			var followRead = taskResultCategories.Where(t => t.ModuleId == 10).FirstOrDefault();
			//如果存在同步跟读,则默认追加一份报听写模块
			if (null != followRead)
			{
				taskResultCategories.Add(new CiWong.Tools.Package.DataContracts.TaskResultCategoryContract()
				{
					Id = followRead.Id,
					ModuleId = 30,
					Name = "报听写"
				});
			}

			var jsonData = taskResultCategories.Where(t => taskResultContentDict.ContainsKey(t.Id)).Select(t => new
			{
				moduleInfo = new
				{
					packageCatalogueId = cId ?? string.Empty,
					moduleId = t.ModuleId,
					moduleName = t.Name ?? string.Empty
				},
				resourceList = ToolsHelper.ResourceList(taskResultContentDict[t.Id], t.ModuleId).Select(m => new
				{
					parentVersionId = m.Id != null ? m.Id.Value : 0,
					versionId = m.VersionId ?? 0,
					resourceName = m.Name ?? string.Empty,
					resourceType = m.ModuleId ?? Guid.Empty
				})
			});

			return jsonData;
		}

		/// <summary>
		/// 获取目录的下载信息
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="cId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic catalogue_info(long packageId, string cId)
		{
			var catalogue = packageService.GetCataloguesForApi(packageId, false).FirstOrDefault(x => x.ID == cId);

			if (null == catalogue)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5011, "当前目录不存在");
			}

			var package = packageService.GetPackageForApi(packageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "未找到指定的资源包");
			}

			var downLoadUrl = packageService.GetOfflinePackageInfo(packageId, cId).FirstOrDefault();

			return new
			{
				packageId = packageId,
				packageName = package.BookName,
				packageCover = package.Cover,
				cId = cId,
				cName = catalogue.Name,
				downLoadUrl = null == downLoadUrl ? string.Empty : downLoadUrl.Url
			};
		}

		/// <summary>
		/// 根据二维码获取离线资源包基础信息[已通过路由配置短地址]
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic qr_resource_info(string url)
		{ 
			if (!url.StartsWith("http://ew.ciwong.com/qr/"))
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5012, "当前URL信息系统不支持");
			}

			string id = url.Replace("http://ew.ciwong.com/qr/", string.Empty);

			var codeContent = new CodeService().GetCodeContents(id, string.Empty);

			if (null == codeContent)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5007, "未找到指定的二维码");
			}

			var package = packageService.GetPackageForApi(codeContent.PackageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "二维码信息错误,未找到指定的资源包");
			}

			if (!codeContent.Content.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5009, "二维码尚未填充资源");
			}

			var cId = codeContent.Content.First().PackageCatalogueId;

			var downLoadUrl = packageService.GetOfflinePackageInfo(codeContent.PackageId, cId).FirstOrDefault();

			if (null == downLoadUrl || string.IsNullOrWhiteSpace(downLoadUrl.Url))
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5010, "当前二维码尚未生成离线资源包,key=" + id);
			}

			var catalogue = packageService.GetCataloguesForApi(codeContent.PackageId, false).FirstOrDefault(x => x.ID == cId);

			if (null == catalogue)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5011, "当前目录不存在");
			}

			return new
			{
				url = url,
				codeName = codeContent.Name,
				packageId = codeContent.PackageId,
				packageName = package.BookName,
				packageCover = package.Cover,
				cId = cId,
				cName = catalogue.Name,
				downLoadUrl = downLoadUrl.Url
			};
		}


		/// <summary>
		/// 根据二维码获取离线资源包基础信息[已通过路由配置短地址]
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic qr_download_package_info(string id)
		{
			var codeContent = new CodeService().GetCodeContents(id, string.Empty);

			if (null == codeContent)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5007, "未找到指定的二维码");
			}

			var package = packageService.GetPackageForApi(codeContent.PackageId);

			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5001, "二维码信息错误,未找到指定的资源包");
			}

			if (!codeContent.Content.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5009, "二维码尚未填充资源");
			}

			var cId = codeContent.Content.First().PackageCatalogueId;

			var downLoadUrl = packageService.GetOfflinePackageInfo(codeContent.PackageId, cId).FirstOrDefault();
			
			if (null == downLoadUrl || string.IsNullOrWhiteSpace(downLoadUrl.Url))
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5010, "当前二维码尚未生成离线资源包,key=" + id);
			}

			var catalogue = packageService.GetCataloguesForApi(codeContent.PackageId, false).FirstOrDefault(x => x.ID == cId);

			if (null == catalogue)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5011, "当前目录不存在");
			}

			return new
			{
				url = "http://ew.ciwong.com/qr/" + codeContent.Id,
				codeName = codeContent.Name,
				packageId = codeContent.PackageId,
				packageName = package.BookName,
				packageCover = package.Cover,
				cId = cId,
				cName = catalogue.Name,
				downLoadUrl = downLoadUrl.Url
			};
		}

		[HttpGet]
		public dynamic File(long packageId, string cid)
		{
			string fileUrl = "";
			string currDirectory = string.Concat("catalogue_", packageId, "_", cid.Trim());

			ToolsHelper.CreateResourceDirectory(packageId, cid, currDirectory);
			ToolsHelper.CreateMainInfo(packageService, packageId, cid, currDirectory);
			ToolsHelper.CreateQrCodeInfo(new CodeService(), packageId, cid, currDirectory);
			ToolsHelper.CreateCatalogueResources(packageService, packageId, cid,currDirectory, ref fileUrl);

			return fileUrl;
		}
	}
}
