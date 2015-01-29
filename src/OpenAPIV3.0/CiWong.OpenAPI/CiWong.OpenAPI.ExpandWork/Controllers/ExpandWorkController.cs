using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.OpenAPI.ExpandWork.DTO;
using CiWong.Resource.Preview.DataContracts;
using CiWong.Resource.Preview.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.ExpandWork.Controllers
{
	public class ExpandWorkController : ApiController
	{

		#region POST数据模拟
		[HttpGet]
		public WorkPackageDTO test()
		{
			var workPackage = new WorkPackageDTO()
			{
				AppId = 20002,
				PackageId = 1410,
				PackageName = "深圳七年级",
				PackageType = 3,
				ProductId = 2349
			};
			var list = new List<WorkResourceDTO>();
			list.Add(new WorkResourceDTO()
			{
				TaskId = "791",
				ModuleId = 7,
				VersionId = 5591825773073883860,
				RelationPath = "5364386369650785552/5591825773073883860",
				SonModuleId = "05a3bf23-b65b-4d7f-956c-5db2b76b9c11",
				ResourceName = "【文字新闻】 郭美美在京实体店关张 原址变身成人用品店(图)",
				ResourceType = "05a3bf23-b65b-4d7f-956c-5db2b76b9c11",
				RequirementContent = string.Empty
			});
			list.Add(new WorkResourceDTO()
			{
				TaskId = "791",
				ModuleId = 10,
				VersionId = 4992341320180472910,
				RelationPath = "4992341320180472910",
				SonModuleId = "a7527f97-14e6-44ef-bf73-3039033f128e",
				ResourceName = "【课后单词表】 111",
				ResourceType = "a7527f97-14e6-44ef-bf73-3039033f128e",
				RequirementContent = "{\"readtimes\":1,\"passscorce\":0,\"speekingtype\":0}",
				resourceParts = new List<ResourcePartDTO>()
				{
					new ResourcePartDTO()
					{
						VersionId = 5325228502561083016,
						ResourceType = "a7527f97-14e6-44ef-bf73-3039033f128e"
					},
					new ResourcePartDTO()
					{
						VersionId = 4700393524673334543,
						ResourceType = "a7527f97-14e6-44ef-bf73-3039033f128e"
					}
				}
			});
			workPackage.workResources = list;

			var com = JSONHelper.Encode<WorkPackageDTO>(workPackage);

			var newp = JSONHelper.Decode<WorkPackageDTO>(com);

			return workPackage;
		}

		[HttpGet]
		public dynamic fileTest()
		{
			var list = new List<WorkFileDTO>();

			list.Add(new WorkFileDTO()
			{
				FileName = "我的图片.jpg",
				FileType = 1,
				FileExt = ".jpg",
				FileUrl = "http://img1.ciwong.net/yishang/6b5a1fa5827d451a6374ac5fd5baed93.jpg",
				FileDesc = string.Empty
			});
			list.Add(new WorkFileDTO()
			{
				FileName = "我的音频.jpg",
				FileType = 2,
				FileExt = ".mp3",
				FileUrl = "http://img1.ciwong.net/yishang/6b5a1fa5827d451a6374ac5fd5baed93.mp3",
				FileDesc = string.Empty
			});

			return list;
		}
		#endregion

		/// <summary>
		/// 创建电子书,电子报作业资源包
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public long create_work_package()
		{
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			string workType = request["worktype"] ?? string.Empty;
			string packageInfo = request["packageInfo"] ?? string.Empty;

			if (workType != "101" && workType != "102")
			{
				throw new ApiArgumentException("参数workType不在指定的范围内", 1);
			}
			if (string.IsNullOrWhiteSpace(packageInfo))
			{
				throw new ApiArgumentException("参数packagesInfo不能为空", 2);
			}
			WorkPackageDTO workPackage = null;
			try
			{
				workPackage = JSONHelper.Decode<WorkPackageDTO>(System.Web.HttpUtility.UrlDecode(packageInfo));
			}
			catch (Exception e)
			{
				throw new ApiException(RetEum.ApplicationError, 3, "序列化失败,message:" + e.Message);
			}
			if (workPackage.PackageId < 1)
			{
				throw new ApiArgumentException("参数packageInfo.packageId不正确", 5);
			}
			if (string.IsNullOrWhiteSpace(workPackage.PackageName))
			{
				throw new ApiArgumentException("参数packageInfo.packageName不能为空", 6);
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var publishRecord = new PublishRecordContract()
			{
				AppId = workPackage.AppId,
				ProductId = workPackage.ProductId.ToString(),
				PackageId = workPackage.PackageId,
				PackageName = workPackage.PackageName,
				PackageType = workPackage.PackageType,
				UserId = userId,
				UserName = string.Empty,
				CreateDate = DateTime.Now
			};

			publishRecord.workResources = workPackage.workResources.Select(t => new WorkResourceContract()
			{
				PackageId = workPackage.PackageId,
				TaskId = t.TaskId,
				ModuleId = t.ModuleId,
				VersionId = t.VersionId,
				RelationPath = t.RelationPath,
				SonModuleId = t.SonModuleId,
				ResourceName = t.ResourceName,
				ResourceType = t.ResourceType,
				RequirementContent = t.RequirementContent,
				resourceParts = t.resourceParts.Select(x => new ResourcePartsContract()
				{
					VersionId = x.VersionId,
					ResourceType = x.ResourceType
				}).ToList()
			}).ToList();

			var recordId = new WorkService().CreateWorkPackage(publishRecord);
			if (recordId < 1)
			{
				throw new ApiException(RetEum.ApplicationError, 6, "创建作业资源包失败");
			}
			return recordId;
		}

		/// <summary>
		/// 创建附件作业
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public long create_filework()
		{
			return 0;
		}

		/// <summary>
		/// 提交跟读作业答案
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public dynamic submit_followread_work()
		{
			var content = Request.GetBodyContent();

			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException("未接收到post数据", 1);
			}
			UnitWorkDTO<FollowReadAnswerDTO> unitWorkAnswer;
			try
			{
				unitWorkAnswer = JSONHelper.Decode<UnitWorkDTO<FollowReadAnswerDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiException(RetEum.ApplicationError, 2, "序列化失败,message:" + e.Message);
			}

			var _workBaseService = new WorkBaseService();
			var _workService = new WorkService();
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var doWorkBase = _workBaseService.GetDoWorkBase(unitWorkAnswer.DoWorkId);
			if (null == doWorkBase)
			{
				return new ApiException(RetEum.ApplicationError, 3, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiException(RetEum.ApplicationError, 4, "当前用户没有提交作业权限");
			}
			var workResource = _workService.GetWorkResource(unitWorkAnswer.ContentId);
			if (null == workResource)
			{
				return new ApiException(RetEum.ApplicationError, 5, "未找到指定的作业资源");
			}
			if (workResource.ModuleId != 10)
			{
				return new ApiException(RetEum.ApplicationError, 6, "错误的资源类型");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workResource.RecordId) == -1)
			{
				return new ApiException(RetEum.ApplicationError, 7, " 作业参数不匹配");
			}

			var unitWork = _workService.GetUserUnitWork(unitWorkAnswer.ContentId, unitWorkAnswer.DoWorkId) ?? new UnitWorksContract();
			unitWork.ContentId = workResource.ContentId;
			unitWork.RecordId = workResource.RecordId;
			unitWork.WorkId = doWorkBase.WorkID;
			unitWork.DoWorkId = doWorkBase.DoWorkID;
			unitWork.SubmitUserId = doWorkBase.SubmitUserID;
			unitWork.SubmitUserName = doWorkBase.SubmitUserName;
			unitWork.ActualScore = unitWorkAnswer.WorkAnswers.Sum(t => t.Score) / unitWorkAnswer.WorkAnswers.SelectMany(t => t.Answers).Count();
			unitWork.SubmitDate = DateTime.Now;
			unitWork.IsTimeOut = unitWork.SubmitDate > doWorkBase.EffectiveDate;
			unitWork.SubmitCount = unitWork.SubmitCount + 1;
			unitWork.WorkLong = unitWork.WorkLong + unitWorkAnswer.WorkLong;
			unitWork.WorkLong = unitWork.WorkLong < 1 ? 1 : unitWork.WorkLong;
			unitWork.Status = 3;

			var answerList = unitWorkAnswer.WorkAnswers.Select(t =>
			{
				int sid = 0;
				string answerContent = JSONHelper.Encode<IEnumerable<ReadAnswerEntity>>(t.Answers.Select(x => new ReadAnswerEntity()
				{
					Sid = sid++,
					Word = x.Word ?? string.Empty,
					AudioUrl = x.AudioUrl ?? string.Empty,
					ReadTimes = x.ReadTimes
				}));
				return new WorkAnswerContract()
				{
					VersionId = t.VersionId,
					ResourceType = t.ResourceType,
					Score = t.Score,
					Assess = t.Assess,
					AnswerContent = answerContent
				};
			});

			var doId = _workService.DoUnitWorks(unitWork, answerList, doWorkBase.TotalNum);

			if (doId < 1)
			{
				return new ApiException(RetEum.ApplicationError, 8, "跟读作业提交失败");
			}

			return doId;
		}

		/// <summary>
		/// 提交模考作业答案
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public dynamic submit_listenspeak_work()
		{
			var content = Request.GetBodyContent();

			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException("未接收到post数据", 1);
			}
			UnitWorkDTO<ListenSpeakAnswerDTO> unitWorkAnswer;
			try
			{
				unitWorkAnswer = JSONHelper.Decode<UnitWorkDTO<ListenSpeakAnswerDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiException(RetEum.ApplicationError, 2, "序列化失败,message:" + e.Message);
			}

			var _workBaseService = new WorkBaseService();
			var _workService = new WorkService();
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var doWorkBase = _workBaseService.GetDoWorkBase(unitWorkAnswer.DoWorkId);
			if (null == doWorkBase)
			{
				return new ApiException(RetEum.ApplicationError, 3, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiException(RetEum.ApplicationError, 4, "当前用户没有提交作业权限");
			}
			var workResource = _workService.GetWorkResource(unitWorkAnswer.ContentId);
			if (null == workResource)
			{
				return new ApiException(RetEum.ApplicationError, 5, "未找到指定的作业资源");
			}
			if (workResource.ModuleId != 10)
			{
				return new ApiException(RetEum.ApplicationError, 6, "错误的资源类型");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workResource.RecordId) == -1)
			{
				return new ApiException(RetEum.ApplicationError, 7, " 作业参数不匹配");
			}
			var unitWork = _workService.GetUserUnitWork(unitWorkAnswer.ContentId, unitWorkAnswer.DoWorkId) ?? new UnitWorksContract();

			if (unitWork.Status == 2 || unitWork.Status == 3)
			{
				return new ApiException(RetEum.ApplicationError, 8, "作业已经完成,不允许重复提交");
			}

			unitWork.ContentId = workResource.ContentId;
			unitWork.RecordId = workResource.RecordId;
			unitWork.WorkId = doWorkBase.WorkID;
			unitWork.DoWorkId = doWorkBase.DoWorkID;
			unitWork.SubmitUserId = doWorkBase.SubmitUserID;
			unitWork.SubmitUserName = doWorkBase.SubmitUserName;
			unitWork.ActualScore = unitWorkAnswer.WorkAnswers.Sum(t => t.Score) / unitWorkAnswer.WorkAnswers.SelectMany(t => t.Answers).Count();
			unitWork.ActualScore = Math.Round(unitWork.ActualScore, 2);
			unitWork.SubmitDate = DateTime.Now;
			unitWork.IsTimeOut = unitWork.SubmitDate > doWorkBase.EffectiveDate;
			unitWork.SubmitCount = unitWork.SubmitCount + 1;
			unitWork.WorkLong = unitWork.WorkLong + unitWorkAnswer.WorkLong;
			unitWork.WorkLong = unitWork.WorkLong < 1 ? 1 : unitWork.WorkLong;
			unitWork.Status = 3;

			var answerList = unitWorkAnswer.WorkAnswers.Select(t =>
			{
				int sid = 0;
				string answerContent = JSONHelper.Encode<IEnumerable<ListenAnswerEntity>>(t.Answers.Select(x => new ListenAnswerEntity()
				{
					Sid = sid++,
					OptionId = x.OptionId,
					AudioUrl = x.AudioUrl ?? string.Empty,
					BlankContent = x.BlankContent ?? string.Empty
				}));
				return new WorkAnswerContract()
				{
					VersionId = t.VersionId,
					ResourceType = t.ResourceType,
					Score = t.Score,
					Assess = t.Assess,
					AnswerContent = answerContent
				};
			});

			var doId = _workService.DoUnitWorks(unitWork, answerList, doWorkBase.TotalNum);

			if (doId < 1)
			{
				return new ApiException(RetEum.ApplicationError, 8, "跟读作业提交失败");
			}

			return doId;
		}

		/// <summary>
		/// 学生获取电子书,电子报作业详细内容列表
		/// </summary>
		[HttpGet]
		public dynamic my_unitworks(long recordId, long doWorkId)
		{
			var _workService = new WorkService();
			var workResources = _workService.GetWorkResources(recordId);

			if (!workResources.Any())
			{
				return Enumerable.Empty<object>();
			}

			var doWorkBase = new WorkBaseService().GetDoWorkBase(doWorkId);

			if (null == doWorkBase)
			{
				return new ApiArgumentException("未找到指定的作业", 1);
			}

			var unitWokrs = _workService.GetUserUnitWorks(doWorkId).ToDictionary(c => c.ContentId, c => c);

			return workResources.Select(t =>
			{
				var unitWork = unitWokrs.ContainsKey(t.ContentId) ? unitWokrs[t.ContentId] : new UnitWorksContract();
				return new
				{
					doId = unitWork.DoId,
					contentId = t.ContentId,
					packageId = t.PackageId,
					taskId = t.TaskId ?? string.Empty,
					moduleId = t.ModuleId,
					versionId = t.VersionId,
					parentVersionId = GetRootVersionId(t.RelationPath),
					sonModuleId = t.SonModuleId,
					resourceName = t.ResourceName,
					resourceType = t.ResourceType,
					actualScore = unitWork.ActualScore,
					workScore = unitWork.WorkScore,
					isTimeOut = unitWokrs.ContainsKey(t.ContentId) && (unitWork.Status == 2 || unitWork.Status == 3) ? unitWork.IsTimeOut : doWorkBase.EffectiveDate < DateTime.Now,
					comment = unitWork.Comment ?? string.Empty,
					commentType = unitWork.CommentType,
					workStatus = unitWork.Status
				};
			});
		}

		private string GetRootVersionId(string relationPath)
		{
			if (relationPath.IndexOf("/") > -1)
			{
				return relationPath.Split('/')[0];
			}
			return "0";
		}
	}
}
