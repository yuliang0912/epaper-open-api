using CiWong.Examination.API;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.Tools.Package;
using CiWong.Tools.Package.DataContracts;
using CiWong.Tools.Package.Services;
using CiWong.Tools.Workshop.DataContracts;
using CiWong.Tools.Workshop.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using ResourceContract = CiWong.Tools.Package.DataContracts.ResourceContract;

namespace CiWong.OpenAPI.ToolsAndPackage.Helper
{
	public static class ToolsHelper
	{
		public static readonly List<int> newsPaperModuleSortArray = new List<int> { 7, 10, 15, 18, 9, 5, 8 };

		/// <summary>
		/// 基础目录
		/// </summary>
		public static string baseDirectory
		{
			get
			{
				return AppDomain.CurrentDomain.BaseDirectory + "/filePackage";
			}
		}

		#region 文件操作相关方法
		/// <summary>
		/// 创建文件夹
		/// </summary>
		/// <param name="path">文件夹路径</param>
		public static void CreateDirectory(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		/// <summary>
		/// 创建文件夹
		/// </summary>
		/// <param name="path">文件夹路径</param>
		/// <param name="directoryName">文件夹名称</param>
		public static void CreateDirectory(string path, string directoryName)
		{
			string filePath = path + "/" + directoryName;

			if (!Directory.Exists(filePath))
			{
				Directory.CreateDirectory(filePath);
			}
		}


		/// <summary>
		/// 创建一个文件,并且写入内容
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fileName"></param>
		/// <param name="fileContent"></param>
		public static void CreateFile(string path, string fileName, string fileContent)
		{
			string filePath = path + "/" + fileName;

			File.WriteAllText(filePath, fileContent, new System.Text.UTF8Encoding(false));
		}
		#endregion

		/// <summary>
		/// 创建离线资源包基础目录结构
		/// </summary>
		public static void CreateResourceDirectory(long packageId, string cid)
		{
			var catalogueDirectory = string.Concat(baseDirectory, "/catalogue_", packageId, "_", cid.Trim());

			CreateDirectory(baseDirectory);
			CreateDirectory(catalogueDirectory);
			CreateDirectory(catalogueDirectory, "resource");
			CreateDirectory(catalogueDirectory, "media");
		}

		/// <summary>
		/// 创建主json说明
		/// </summary>
		/// <param name="packageService"></param>
		/// <param name="packageId"></param>
		/// <param name="cid"></param>
		public static void CreateMainInfo(PackageService packageService, long packageId, string cid)
		{
			var package = packageService.GetPackageForApi(packageId);

			if (null == package || package.GroupType == 2 || package.GroupType == 4)
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5006, "未找到指定的资源包或者资源包格式不正确");
			}

			var catalogueDirectory = string.Concat(baseDirectory, "/catalogue_", packageId, "_", cid.Trim());

			string fileName = DownLoadFile(catalogueDirectory, package.Cover);

			var mainInfo = new
			{
				packageId = package.PackageId,
				packageName = package.BookName,
				packageType = package.GroupType,
				cover = package.Cover.Replace(package.Cover, "media/" + fileName),
				price = package.Price,
				subjectId = package.SubjectId,
				periodId = package.PeriodId,
				gradeId = package.GradeId,
				teamId = package.TeamId,
				teamName = package.TeamName,
				currCatalogueId = cid,
				jsonVersion = "1.0",
				crateTime = DateTime.Now
			};

			ToolsHelper.CreateFile(catalogueDirectory, "main.json", JSONHelper.Encode<object>(mainInfo));
		}

		/// <summary>
		/// 创建目录中的资源
		/// </summary>
		public static void CreateCatalogueResources(PackageService packageService, long packageId, string cid)
		{
			var taskResultCategories = packageService.GetTaskResultCategoriesForApi(packageId, cid);
			var taskResultContents = packageService.GetTaskResultContentsForApi(packageId, cid, false).OrderBy(t => t.DisplayOrder).ToList();

			if (!taskResultContents.Any() || !taskResultCategories.Any())
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5004, "当前目录中未找到资源");
			}

			var taskResultContentDict = taskResultContents.Where(t => t.ModuleId != 9).GroupBy(c => c.CatalogueId).ToDictionary(c => c.Key, c => c.ToList());


			var allResource = new List<ResourceContract>();

			var jsonData = taskResultCategories.Where(t => taskResultContentDict.ContainsKey(t.Id)).Select(t =>
			{
				var resourceList = ToolsHelper.ResourceList(taskResultContentDict[t.Id]);
				allResource.AddRange(resourceList);
				return new
				{
					moduleInfo = new
					{
						packageCatalogueId = cid ?? string.Empty,
						moduleId = t.ModuleId,
						moduleName = t.Name ?? string.Empty
					},
					resourceList = resourceList.Select(m => new
					{
						parentVersionId = m.Id != null ? m.Id.Value : 0,
						versionId = m.VersionId ?? 0,
						name = m.Name ?? string.Empty,
						resourceModuleId = m.ModuleId ?? Guid.Empty
					})
				};
			});

			var catalogueDirectory = string.Concat(baseDirectory, "/catalogue_", packageId, "_", cid.Trim());

			ToolsHelper.CreateFile(catalogueDirectory, "catalogue.json", JSONHelper.Encode<object>(jsonData));

			CreateResources(allResource, catalogueDirectory, packageId, cid);

			ZipHelper.ZipFileDirectory(catalogueDirectory, baseDirectory + string.Format("/catalogue_{0}_{1}.zip", packageId, cid));
		}

		/// <summary>
		/// 创建资源文件
		/// </summary>
		/// <param name="allResource"></param>
		/// <param name="catalogueDirectory"></param>
		public static void CreateResources(List<ResourceContract> allResource, string catalogueDirectory, long packageId, string cid)
		{
			var jsonData = new object();
			var resourceList = new Dictionary<string, string>();

			#region 各资源模块json数据生成
			foreach (var item in allResource)
			{
				string key = string.Concat(item.ModuleId.Value.ToString(), "_", item.VersionId.Value == 0 ? item.Id.Value : item.VersionId.Value, ".json");
				switch (item.ModuleId.Value.ToString())
				{
					case "05a3bf23-b65b-4d7f-956c-5db2b76b9c11": //时文-文章
						var model = ResourceServices.Instance.GetByVersionId<ArticleContract>(item.VersionId.Value);
						resourceList.Add(key, JSONHelper.Encode<object>(model.Data));
						break;

					case "a7527f97-14e6-44ef-bf73-3039033f128e"://跟读-课后单词表
						var syncFollowRead = ResourceServices.Instance.GetByVersionId<SyncFollowReadContract>(item.Id.Value).Data;
						var wordVersionList = syncFollowRead.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word).SelectMany(t => t.List).Where(t => t.VersionId.HasValue).Select(t => t.VersionId.Value).ToArray();
						var wordList = ResourceServices.Instance.GetByVersionIds<WordContract>(wordVersionList);
						jsonData = wordList.Data.Select(x => new
						{
							wId = x.VersionId ?? 0,
							words = x.Name ?? string.Empty,
							wordFile = ConverAudioUrl(x.AudioUrl),
							wordType = x.IsExpand.HasValue ? x.IsExpand.Value : false,
							symbol = x.Symbol ?? string.Empty,
							syllable = x.Syllable ?? string.Empty,
							pretations = x.Pretations ?? string.Empty,
							sentences = x.Sentences.Any() ? x.Sentences.First().Text ?? string.Empty : string.Empty,
							sentFile = x.Sentences.Any() ? ConverAudioUrl(x.Sentences.First().AudioUrl) : string.Empty,
							wordPic = x.PictureUrl ?? string.Empty
						});
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData)); //课后单词表
						resourceList.Add(string.Concat("3ac07125-31ac-11e5-a511-782bcb066f05", item.VersionId.Value, ".json"), resourceList[key]);//报听写
						break;

					case "992a5055-e9d0-453f-ab40-666b4d7030bb"://跟读-课文
						var syncFollowReadText = ResourceServices.Instance.GetByVersionId<SyncFollowReadTextContract>(item.VersionId.Value).Data;
						jsonData = syncFollowReadText.Sections.SelectMany(t => t.Sentences).Select(x => new
						{
							content = x.Content ?? string.Empty,
							audioUrl = ToolsHelper.ConverAudioUrl(x.AudioUrl),
							versionId = x.VersionId ?? 0,
							resourceModuleId = x.ModuleId ?? Guid.Empty,
							name = x.Name ?? string.Empty,
						});
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData));
						break;

					case "fcfd6131-cdb6-4eb8-9cb9-031f710a8f15"://模考
						var listeningAndSpeaking = ResourceServices.Instance.GetByVersionId<ListeningAndSpeakingContract>(item.VersionId.Value).Data;
						jsonData = new
						{
							totalScore = listeningAndSpeaking.TotalScore,
							limitTime = listeningAndSpeaking.LimitTime,
							versionId = listeningAndSpeaking.VersionId ?? 0,
							moduleId = listeningAndSpeaking.ModuleId ?? Guid.Empty,
							items = listeningAndSpeaking.Items.Select(t => new
							{
								templateSettings = new
								{
									content = t.TemplateSettings.Content ?? string.Empty,
									audioUrl = ToolsHelper.ConverAudioUrl(t.TemplateSettings.AudioUrl),
									questionNumber = t.TemplateSettings.QuestionNumber,
									listeningAndSpeakingRule = new
									{
										audioViews = t.TemplateSettings.ListeningAndSpeakingRule != null ? t.TemplateSettings.ListeningAndSpeakingRule.AudioViews : 0,
										lookTime = t.TemplateSettings.ListeningAndSpeakingRule != null ? t.TemplateSettings.ListeningAndSpeakingRule.LookTime : 0,
										answerTime = t.TemplateSettings.ListeningAndSpeakingRule != null ? t.TemplateSettings.ListeningAndSpeakingRule.AnswerTime : 0
									}
								},
								scores = t.Scores.Select(m => new
								{
									questionVersionId = m.QuestionVersionId,
									score = m.Score
								}),
								questions = t.Questions.Select(m => ToolsHelper.QuestionFunc(m))
							})
						};
						var newListeningAndSpeaking = ListenConvertHepler.ConvertSpeak(listeningAndSpeaking);
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData));
						resourceList.Add(string.Concat("e9430760-9f2e-4256-af76-3bd8980a9de4_", item.VersionId.Value, ".json"), JSONHelper.Encode<object>(newListeningAndSpeaking));
						break;
					default:
						break;
				}
			}
			#endregion

			foreach (var item in resourceList)
			{
				var content = item.Value;
				var urlList = StringExtensions.MatchsFileUrl(item.Value);
				foreach (var url in urlList)
				{
					string fileName = ToolsHelper.DownLoadFile(catalogueDirectory, url);
					content = content.Replace(url, string.Concat("../packages/catalogue_", packageId, "_", cid, "/media/" + fileName));
				}
				ToolsHelper.CreateFile(catalogueDirectory + "/resource", item.Key, content);
			}
		}

		/// <summary>
		/// 下载文件
		/// </summary>
		public static string DownLoadFile(string catalogueDirectory, string fileUrl)
		{
			catalogueDirectory = catalogueDirectory + "\\media\\";

			if (fileUrl.IndexOf("?") > -1)
			{
				fileUrl = fileUrl.Substring(0, fileUrl.LastIndexOf('?'));
			}
			string fileName = fileUrl.Substring(fileUrl.LastIndexOf('/') + 1);

			if (!File.Exists(catalogueDirectory + fileName))
			{
				try
				{
					new WebClient().DownloadFile(fileUrl, catalogueDirectory + fileName);
				}
				catch
				{
					throw new Exception("文件下载失败,URL:" + fileUrl);
				}
			}
			return fileName;
		}

		public static object CatalogueFunc(PackageCatalogueContract m)
		{
			return new
			{
				id = m.ID ?? string.Empty,
				name = m.Name ?? string.Empty,
				children = m.Children != null ? m.Children.Select(t => CatalogueFunc(t)) : Enumerable.Empty<object>()
			};
		}

		public static List<ResourceContract> ResourceList(List<TaskResultContentContract> resuletContents)
		{
			var list = new List<ResourceContract>();

			var resourceModuleId = new Guid(resuletContents.First().ResourceModuleId);

			if (resourceModuleId.Equals(ResourceModuleOptions.SyncFollowRead)) //同步跟读
			{
				var syncFollowReadResult = ResourceServices.Instance.GetByVersionIds<SyncFollowReadContract>(resuletContents.Select(t => t.ResourceVersionId).ToArray());

				if (null == syncFollowReadResult || !syncFollowReadResult.IsSucceed || !syncFollowReadResult.Data.Any())
				{
					return list;
				}

				foreach (var syncFollowRead in syncFollowReadResult.Data)
				{
					foreach (var part in syncFollowRead.Parts)
					{
						if (null == part.List || !part.List.Any())
						{
							continue;
						}
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
				var newsResult = ResourceServices.Instance.GetByVersionIds<NewsContract>(resuletContents.Select(t => t.ResourceVersionId).ToArray());
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
				var syncTrainResult = ResourceServices.Instance.GetByVersionIds<CiWong.Tools.Workshop.DataContracts.SyncTrainContract>(resuletContents.Select(t => t.ResourceVersionId).ToArray());
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
		}

		public static string ConverAudioUrl(string audioUrl, int fileType = 2)
		{
			if (string.IsNullOrWhiteSpace(audioUrl))
			{
				return string.Empty;
			}
			if (fileType == 2 && audioUrl.ToLower().StartsWith("http://img1.ciwong.net"))
			{
				return audioUrl + (audioUrl.IndexOf("?") > -1 ? "&fileType=mp3" : "?fileType=mp3");
			}
			return audioUrl;
		}

		public static object QuestionFunc(QuestionContract m)
		{
			return new
			{
				type = m.Type,
				versionId = m.VersionId ?? 0,
				moduleId = m.ModuleId ?? Guid.Empty,
				trunk = new
				{
					body = m.Trunk != null ? (m.Trunk.Body ?? string.Empty).RemoveHtml() : string.Empty,
					attachments = m.Trunk != null ? m.Trunk.Attachments.Select(p => new
						{
							id = p.Id,
							name = p.Name ?? string.Empty,
							url = ConverAudioUrl(p.Url, p.FileType),
							fileType = p.FileType,
							position = (int)p.Position
						}) : Enumerable.Empty<object>()
				},
				options = m.Options.Select(o => new
				{
					id = o.Id,
					isAnswer = o.IsAnswer ? 1 : 0,
					value = o.Value.Select(u => new
					{
						body = (u.Body ?? string.Empty).RemoveHtml(),
						attachments = u.Attachments != null ? u.Attachments.Select(p => new
							{
								id = p.Id,
								name = p.Name ?? string.Empty,
								url = ConverAudioUrl(p.Url, p.FileType),
								fileType = p.FileType,
								position = (int)p.Position
							}) : Enumerable.Empty<object>()
					})
				}),
				children = m.Children != null ? m.Children.Select(t => QuestionFunc(t)) : Enumerable.Empty<QuestionContract>()
			};
		}
	}
}
