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
				return AppDomain.CurrentDomain.BaseDirectory + "filePackage\\";
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
		public static void CreateResourceDirectory(long packageId, string cid, string fileDirectory)
		{
			var catalogueDirectory = string.Concat(baseDirectory, fileDirectory);

			CreateDirectory(baseDirectory);
			CreateDirectory(catalogueDirectory);
			CreateDirectory(catalogueDirectory, "resource");
			CreateDirectory(catalogueDirectory, "answer");
			CreateDirectory(catalogueDirectory, "media");
		}

		/// <summary>
		/// 创建主json说明
		/// </summary>
		/// <param name="packageService"></param>
		/// <param name="packageId"></param>
		/// <param name="cid"></param>
		public static void CreateMainInfo(PackageService packageService, long packageId, string cid, string fileDirectory)
		{
			var package = packageService.GetPackageForApi(packageId);

			if (null == package || package.GroupType == 2 || package.GroupType == 4)
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5004, "未找到指定的资源包或者资源包格式不正确");
			}

			var catalogueDirectory = string.Concat(baseDirectory, fileDirectory);

			string fileName = string.IsNullOrEmpty(package.Cover) ? "" : DownLoadFile(catalogueDirectory, package.Cover);

			var mainInfo = new
			{
				packageId = package.PackageId,
				packageName = package.BookName,
				packageType = package.GroupType,
				cover = string.IsNullOrEmpty(package.Cover) ? "" : package.Cover.Replace(package.Cover, "media/" + fileName),
				price = package.Price,
				subjectId = package.SubjectId,
				periodId = package.PeriodId,
				gradeId = package.GradeId,
				teamId = package.TeamId,
				teamName = package.TeamName,
				cId = cid,
				catalogueFile = fileDirectory + "/catalogue.json",
				qrCodeFile = fileDirectory + "/qrCode.json",
				createTime = DateTime.Now,
				jsonVersion = "1.0"
			};

			ToolsHelper.CreateFile(catalogueDirectory, "main.json", JSONHelper.Encode<object>(mainInfo));
		}

		/// <summary>
		/// 创建当前目录中用到的二维码信息
		/// </summary>
		/// <param name="codeService"></param>
		/// <param name="packageId"></param>
		/// <param name="cid"></param>
		public static void CreateQrCodeInfo(CodeService codeService, long packageId, string cid, string fileDirectory)
		{
			var list = codeService.GetCodeByPackageId(packageId, cid);

			var jsonData = list.Select(m => new
			{
				url = "http://ew.ciwong.com/qr/" + m.Id,
				codeName = m.Name,
				packageId = packageId,
				cId = cid,
				type = 9,
				createTime = DateTime.Now,
				resourceList = m.Content.Select(x =>
				{
					var resourceVersionInfo = ToolsHelper.GetVersionInfo(x.ResourceVersionId);

					return new
					{
						versionId = resourceVersionInfo.Item1,
						parentVersion = resourceVersionInfo.Item2,
						resourceName = x.ResourceName,
						resourceType = ToolsHelper.GetModuleInfo(x.ResourceModuleId),
						moduleId = Convert.ToInt32(x.ModuleId),
						resourceFile = GetFileName(resourceVersionInfo.Item1, resourceVersionInfo.Item2, ToolsHelper.GetModuleInfo(x.ResourceModuleId), false, fileDirectory + "/resource/"),
						refAnswerFile = GetFileName(resourceVersionInfo.Item1, resourceVersionInfo.Item2, ToolsHelper.GetModuleInfo(x.ResourceModuleId), true, fileDirectory + "/answer/")
					};
				})
			});

			var catalogueDirectory = string.Concat(baseDirectory, fileDirectory);
			ToolsHelper.CreateFile(catalogueDirectory, "qrCode.json", JSONHelper.Encode<object>(jsonData));
		}

		/// <summary>
		/// 创建目录中的资源
		/// </summary>
		public static void CreateCatalogueResources(PackageService packageService, long packageId, string cid, string fileDirectory, ref string zipedFilePath)
		{
			var taskResultCategories = packageService.GetTaskResultCategoriesForApi(packageId, cid);
			var taskResultContents = packageService.GetTaskResultContentsForApi(packageId, cid, false).OrderBy(t => t.DisplayOrder).ToList();

			if (!taskResultContents.Any() || !taskResultCategories.Any())
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5004, "当前目录中未找到资源");
			}

			var taskResultContentDict = taskResultContents.GroupBy(c => c.CatalogueId).ToDictionary(c => c.Key, c => c.ToList());

			var followRead = taskResultCategories.Where(t => t.ModuleId == 10).FirstOrDefault();
			//如果存在同步跟读,则默认追加一份报听写模块
			if (null != followRead)
			{
				taskResultCategories.Add(new TaskResultCategoryContract()
				{
					Id = followRead.Id,
					ModuleId = 30,
					Name = "报听写"
				});
			}

			var allResource = new List<ResourceContract>();

			var jsonData = taskResultCategories.Where(t => taskResultContentDict.ContainsKey(t.Id)).Select(t =>
			{
				var resourceList = ToolsHelper.ResourceList(taskResultContentDict[t.Id], t.ModuleId);
				allResource.AddRange(resourceList);
				return new
				{
					moduleInfo = new
					{
						cId = cid ?? string.Empty,
						moduleId = t.ModuleId,
						moduleName = t.Name ?? string.Empty
					},
					resourceList = resourceList.Select(m => new
					{
						parentVersionId = m.Id.Value,
						versionId = m.VersionId,
						resourceName = m.Name ?? string.Empty,
						resourceType = m.ModuleId ?? Guid.Empty,
						resourceFile = GetResourceFile(m, fileDirectory + "/resource/"),
						refAnswerFile = GetFileName(m.VersionId.Value, m.Id.Value, m.ModuleId.Value.ToString(), true, fileDirectory + "/answer/")
					})
				};
			});

			var catalogueDirectory = string.Concat(baseDirectory, fileDirectory);

			ToolsHelper.CreateFile(catalogueDirectory, "catalogue.json", JSONHelper.Encode<object>(jsonData));

			CreateResources(allResource, fileDirectory, packageId, cid);
			zipedFilePath = baseDirectory + fileDirectory + ".zip";
			ZipHelper.ZipFileDirectory(catalogueDirectory, zipedFilePath);
		}

		/// <summary>
		/// 创建资源文件
		/// </summary>
		/// <param name="allResource"></param>
		/// <param name="catalogueDirectory"></param>
		public static void CreateResources(List<ResourceContract> allResource, string fileDirectory, long packageId, string cid)
		{
			var jsonData = new object();
			var resourceList = new Dictionary<string, string>();
			var answerList = new Dictionary<string, string>();
			var catalogueDirectory = string.Concat(baseDirectory, fileDirectory);

			#region 各资源模块json数据生成
			foreach (var item in allResource)
			{
				string key = GetResourceFile(item);
				if (string.IsNullOrEmpty(key) || resourceList.ContainsKey(key))
				{
					continue;
				}
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
						//报听写不需要批改了.注释
						//var workAnswer = wordList.Data.Select(x => new
						//{
						//	versionId = x.VersionId ?? 0,
						//	word = x.Name ?? string.Empty
						//});
						//answerList.Add(key.Replace(item.ModuleId.Value.ToString(), "3ac07125-31ac-11e5-a511-782bcb066f05"), JSONHelper.Encode<object>(jsonData));
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData)); //同步跟读
						resourceList.Add(key.Replace(item.ModuleId.Value.ToString(), "3ac07125-31ac-11e5-a511-782bcb066f05"), resourceList[key]);//报听写
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
						//模考转换成试卷JSON
						answerList.Add(key, JSONHelper.Encode<object>(newListeningAndSpeaking));
						//模考原始JSON
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData));
						//模考转换成试卷JSON
						resourceList.Add(key.Replace(item.ModuleId.Value.ToString(), "e9430760-9f2e-4256-af76-3bd8980a9de4"), answerList[key]);
						break;

					case "1f693f76-02f5-4a40-861d-a8503df5183f": //试卷
						var examApi = DependencyResolver.Current.GetService<IExaminationAPI>();
						var examination = examApi.GetExaminationModel(item.VersionId.Value);//634828917904191909
						var subQuestionList = WikiQuesConvertHelper.GetXiaotiList(examination);
						var questionList = new List<CiWong.Resource.Preview.DataContracts.QuestionContract>();

						foreach (var question in subQuestionList)
						{
							if (question.Attachments.Any(x => x.FileType == 2))
							{
								questionList.Add(question);
							}
						}
						jsonData = questionList.Select(x => new
						{
							sid = x.Sid,
							versionId = x.VersionId,
							attachments = x.Attachments.Where(m => m.FileType == 2).Select(n => new
							{
								fileUrl = n.FileUrl,
								fileType = n.FileType
							})
						});
						resourceList.Add(key, JSONHelper.Encode<object>(jsonData));
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
					string fileName = string.IsNullOrEmpty(url) ? "" : ToolsHelper.DownLoadFile(catalogueDirectory, url);
					//时文和模考试卷版做特殊处理,便于网页直接获取相对路径的文件
					if (item.Key.StartsWith("05a3bf23-b65b-4d7f-956c-5db2b76b9c11") || item.Key.StartsWith("e9430760-9f2e-4256-af76-3bd8980a9de4"))
					{
						content = string.IsNullOrEmpty(url) ? content : content.Replace(url, string.Concat("../packages/", fileDirectory, "/media/" + fileName));
					}
					else
					{
						content = string.IsNullOrEmpty(url) ? content : content.Replace(url, string.Concat(fileDirectory, "/media/", fileName));
					}
				}
				if (!string.IsNullOrEmpty(item.Key))
				{
					ToolsHelper.CreateFile(catalogueDirectory + "/resource", item.Key, content);
				}
			}

			foreach (var item in answerList)
			{
				ToolsHelper.CreateFile(catalogueDirectory + "/answer", item.Key, item.Value);
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

		public static object CatalogueFunc(PackageCatalogueContract m, Dictionary<string, string> downLoadUrls)
		{
			return new
			{
				id = m.ID ?? string.Empty,
				name = m.Name ?? string.Empty,
				downLoadUrl = downLoadUrls.ContainsKey(m.ID) ? downLoadUrls[m.ID] : string.Empty,
				children = m.Children != null ? m.Children.Select(t => CatalogueFunc(t, downLoadUrls)) : Enumerable.Empty<object>()
			};
		}

		public static List<ResourceContract> ResourceList(List<TaskResultContentContract> resuletContents, int moduleId)
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
						if (part.ModuleId == ResourceModuleOptions.Word && moduleId == 10)
						{
							list.Add(new ResourceContract()
							{
								Id = syncFollowRead.VersionId ?? 0,
								VersionId = 0,
								Name = string.Format("【{0}】 {1}", "课后单词表", syncFollowRead.Name),
								ModuleId = part.ModuleId
							});
						}
						else if (part.ModuleId == ResourceModuleOptions.Word && moduleId == 30)
						{
							list.Add(new ResourceContract()
							{
								Id = syncFollowRead.VersionId ?? 0,
								VersionId = 0,
								Name = string.Format("【{0}】 {1}", "报听写", syncFollowRead.Name),
								ModuleId = new Guid("3ac07125-31ac-11e5-a511-782bcb066f05")
							});
						}
						else if (part.ModuleId == ResourceModuleOptions.SyncFollowReadText && moduleId == 10)
						{
							list.AddRange(part.List.Select(t => new ResourceContract()
							{
								Id = syncFollowRead.VersionId ?? 0,
								VersionId = t.VersionId ?? 0,
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
						if (part.Id == "2" || null == part.List || !part.List.Any())
						{
							continue;
						}

						list.AddRange(part.List.Select(t => new ResourceContract()
						{
							Id = item.VersionId ?? 0,
							VersionId = t.VersionId ?? 0,
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
							Id = item.VersionId ?? 0,
							VersionId = t.VersionId ?? 0,
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

		public static Tuple<long, long> GetVersionInfo(string versionStr)
		{
			if (string.IsNullOrWhiteSpace(versionStr))
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5008, "二维码数据格式错误");
			}

			var versionList = versionStr.Split('_').Select(c => Convert.ToInt64(c));
			if (versionList.Count() > 1)
			{
				return new Tuple<long, long>(versionList.Last(), versionList.First());
			}
			else
			{
				return new Tuple<long, long>(versionList.Last(), 0);
			}
		}

		public static string GetModuleInfo(string moduleStr)
		{
			if (string.IsNullOrWhiteSpace(moduleStr))
			{
				throw new ApiArgumentException(ErrorCodeEum.Resource_5008, "二维码数据格式错误");
			}

			return moduleStr.Split('_').Last();
		}

		private static string GetResourceFile(ResourceContract resource, string path = null)
		{
			string key = string.Empty;
			var currResourceModuleId = resource.ModuleId.Value.ToString();

			if (currResourceModuleId == "05a3bf23-b65b-4d7f-956c-5db2b76b9c11" || currResourceModuleId == "a7527f97-14e6-44ef-bf73-3039033f128e" || currResourceModuleId == "992a5055-e9d0-453f-ab40-666b4d7030bb" ||
				currResourceModuleId == "fcfd6131-cdb6-4eb8-9cb9-031f710a8f15" || currResourceModuleId == "1f693f76-02f5-4a40-861d-a8503df5183f" || currResourceModuleId == "3ac07125-31ac-11e5-a511-782bcb066f05")
			{
				key = GetFileName(resource.VersionId.Value, resource.Id.Value, resource.ModuleId.Value.ToString());
				if (!string.IsNullOrEmpty(path))
				{
					key = path + key;
				}
			}
			return key;
		}

		private static string GetFileName(long versionId, long parentVersionId, string resourceType, bool isAnswer = false, string path = null)
		{
			var fileName = string.Empty;
			if (isAnswer && resourceType != "fcfd6131-cdb6-4eb8-9cb9-031f710a8f15")
			{
				return fileName;
			}
			else
			{
				fileName = string.Concat(resourceType, "_", versionId == 0 ? parentVersionId : versionId, ".json");
			}
			if (!string.IsNullOrEmpty(path))
			{
				fileName = string.Concat(path, fileName);
			}
			return fileName;
		}
	}
}
