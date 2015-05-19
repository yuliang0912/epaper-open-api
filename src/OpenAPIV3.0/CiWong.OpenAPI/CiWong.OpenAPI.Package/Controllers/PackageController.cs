using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using CiWong.OpenAPI.Core;
using CiWong.Tools.Package;
using CiWong.Tools.Package.DataContracts;
using CiWong.Tools.Package.Services;
using CiWong.Tools.Workshop.DataContracts;
using CiWong.Tools.Workshop.Services;
using ResourceContract = CiWong.Tools.Package.DataContracts.ResourceContract;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class PackageController : ApiController
	{
		private readonly PackageService packageService;
		public static readonly List<int> newsPaperModuleSortArray = new List<int> { 7, 10, 15, 18, 9, 5, 8 };
		public PackageController()
		{
			packageService = new PackageService();
		}

		private object CatalogueFunc(PackageCatalogueContract m)
		{
			return new
			{
				id = m.ID ?? string.Empty,
				name = m.Name ?? string.Empty,
				children =
					m.Children != null
						? m.Children.Select(t => CatalogueFunc(t))
						: Enumerable.Empty<PackageCatalogueContract>()
			};
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
				return new ApiArgumentException("未找到指定的资源包", 1);
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
				gradeId = package.GradeId
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

			if (!string.IsNullOrWhiteSpace(cid))
			{
				result = result.Where(t => t.ID == cid).ToList();
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
				children = x.Children.Select(t => CatalogueFunc(t))
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
		public dynamic book_resources(long packageId, string cId, int moduleId = 0, bool isFilter = true)
		{
			var packageCategoryContent = packageService.GetTaskResultContentsForApi(packageId, cId, false);

			if (null == packageCategoryContent || !packageCategoryContent.Any())
			{
				return new ApiException(RetEum.ApplicationError, 1, "未找到资源");
			}

			if (moduleId > 0)
			{
				packageCategoryContent = packageCategoryContent.Where(t => t.ModuleId == moduleId).ToList();
			}
			if (isFilter)
			{
				packageCategoryContent = packageCategoryContent.Where(t => t.ModuleId != 9).ToList();
			}

			var taskResultContent = packageCategoryContent.
									OrderBy(t => newsPaperModuleSortArray.IndexOf(t.ModuleId)).
									ThenBy(t => t.DisplayOrder).
									GroupBy(t => t.ModuleId).
									ToDictionary(c => c.Key, c => c.ToList());

			Func<List<TaskResultContentContract>, List<ResourceContract>> getResourceList = (resuletContents) =>
			{
				var list = new List<ResourceContract>();

				var resourceModuleId = new Guid(resuletContents.First().ResourceModuleId);

				if (resourceModuleId.Equals(ResourceModuleOptions.SyncFollowRead)) //同步跟读
				{
					var syncFollowReadResult =
						ResourceServices.Instance.GetByVersionIds<SyncFollowReadContract>(
							resuletContents.Select(t => t.ResourceVersionId).ToArray());

					if (null == syncFollowReadResult || !syncFollowReadResult.IsSucceed || !syncFollowReadResult.Data.Any())
					{
						return list;
					}

					foreach (var syncFollowRead in syncFollowReadResult.Data)
					{
						foreach (var part in syncFollowRead.Parts)
						{
							if (part.ModuleId == ResourceModuleOptions.Word)
							{
								list.Add(new ResourceContract()
								{
									Id = syncFollowRead.VersionId,
									VersionId = 0,
									Name = string.Format("【{0}】 {1}", "课后单词表", syncFollowRead.Name),
									ModuleId = part.ModuleId
								});
							}
							else if (part.ModuleId == ResourceModuleOptions.SyncFollowReadText)
							{
								if (null == part.List || !part.List.Any())
								{
									continue;
								}
								list.AddRange(part.List.Select(t => new ResourceContract()
								{
									Id = syncFollowRead.VersionId,
									VersionId = t.VersionId,
									Name = string.Format("【{0}】 {1}", t.Name, syncFollowRead.Name),
									ModuleId = t.ModuleId
								}));
							}
						}
					}
				}
				else if (resourceModuleId.Equals(ResourceModuleOptions.News))//新闻
				{
					var newsResult =
						ResourceServices.Instance.GetByVersionIds<NewsContract>(
							resuletContents.Select(t => t.ResourceVersionId).ToArray());
					if (null == newsResult || !newsResult.IsSucceed || !newsResult.Data.Any())
					{
						return list;
					}
					foreach (var item in newsResult.Data)
					{
						if (null == item.Parts || !item.Parts.Any())
						{
							continue;
						}
						foreach (var part in item.Parts)
						{
							if (null == part.List || !part.List.Any())
							{
								continue;
							}
							list.AddRange(part.List.Select(t => new ResourceContract()
							{
								Id = item.VersionId,
								VersionId = t.VersionId,
								Name = string.Format("【{0}】 {1}", part.Name, t.Name),
								ModuleId = t.ModuleId
							}));
						}
					}
				}
				else if (resourceModuleId.Equals(ResourceModuleOptions.SyncTrain))//同步讲练
				{
					var syncTrainResult =
						ResourceServices.Instance.GetByVersionIds<CiWong.Tools.Workshop.DataContracts.SyncTrainContract>(
							resuletContents.Select(t => t.ResourceVersionId).ToArray());
					if (null == syncTrainResult || !syncTrainResult.IsSucceed || !syncTrainResult.Data.Any())
					{
						return list;
					}
					foreach (var item in syncTrainResult.Data)
					{
						if (null == item.Parts || !item.Parts.Any())
						{
							continue;
						}
						foreach (var part in item.Parts)
						{
							if (null == part.List || !part.List.Any())
							{
								continue;
							}
							list.AddRange(part.List.Select(t => new ResourceContract()
							{
								Id = item.VersionId,
								VersionId = t.VersionId,
								Name = string.Format("【{0}】 {1}", part.Name, t.Name),
								ModuleId = t.ModuleId
							}));
						}
					}
				}
				else //其他不查询子模块内容的
				{
					list = resuletContents.Select(t => new ResourceContract()
					{
						Id = 0,
						VersionId = t.ResourceVersionId,
						Name = t.ResourceName,
						ModuleId = new Guid(t.ResourceModuleId)
					}).ToList();
				}
				return list;
			};

			var jsonData = taskResultContent.Select(t => new
			{
				moduleInfo = new
				{
					packageCatalogueId = cId ?? string.Empty,
					moduleId = t.Key,
					moduleName = t.Value.First().ModuleName ?? string.Empty
				},
				resourceList = getResourceList(t.Value).Select(m => new
				{
					parentVersionId = m.Id != null ? m.Id.Value : 0,
					versionId = m.VersionId ?? 0,
					name = m.Name ?? string.Empty,
					resourceModuleId = m.ModuleId ?? Guid.Empty
				})
			});

			return jsonData;
		}
	}
}
