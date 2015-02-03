using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.OpenAPI.ExpandWork.DTO;
using CiWong.Resource.Preview.DataContracts;
using CiWong.Resource.Preview.Service;
using CiWong.Work.Service;
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
		/// <summary>
		/// 创建电子书,电子报作业资源包
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public dynamic create_work_package()
		{
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			string workType = request["worktype"] ?? string.Empty;
			string packageInfo = request["packageInfo"] ?? string.Empty;

			if (workType != "101" && workType != "102")
			{
				return new ApiArgumentException("参数workType不在指定的范围内", 1);
			}
			if (string.IsNullOrWhiteSpace(packageInfo))
			{
				return new ApiArgumentException("参数packagesInfo不能为空", 2);
			}
			WorkPackageDTO workPackage = null;
			try
			{
				workPackage = JSONHelper.Decode<WorkPackageDTO>(System.Web.HttpUtility.UrlDecode(packageInfo));
			}
			catch (Exception e)
			{
				return new ApiException(RetEum.ApplicationError, 3, "序列化失败,message:" + e.Message);
			}
			if (workPackage.PackageId < 1)
			{
				return new ApiArgumentException("参数packageInfo.packageId不正确", 5);
			}
			if (string.IsNullOrWhiteSpace(workPackage.PackageName))
			{
				return new ApiArgumentException("参数packageInfo.packageName不能为空", 6);
			}
			if (!workPackage.workResources.Any())
			{
				return new ApiArgumentException("作业资源包中没有任何资源", 7);
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
				return new ApiException(RetEum.ApplicationError, 6, "创建作业资源包失败");
			}
			return recordId;
		}

		/// <summary>
		/// 创建附件作业资源包
		/// </summary>
		/// <returns></returns>
		[BasicAuthentication, HttpPost]
		public dynamic create_filework()
		{
			var content = Request.GetBodyContent();

			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException("未接收到post数据", 1);
			}
			List<WorkFileDTO> workFiles = new List<WorkFileDTO>();
			try
			{
				workFiles = JSONHelper.Decode<List<WorkFileDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiException(RetEum.ApplicationError, 2, "序列化失败,message:" + e.Message);
			}
			if (!workFiles.Any())
			{
				return new ApiException(RetEum.ApplicationError, 3, "集合中不包含任何元素");
			}

			var workFilePackage = new WorkFilePackageContract();
			workFilePackage.FilePackageName = "作业快传附件资源包";
			workFilePackage.FilePackageType = 2;
			workFilePackage.UserId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			workFilePackage.UserName = string.Empty;
			workFilePackage.CreateDate = DateTime.Now;

			workFilePackage.WorkFileResources = workFiles.Select(t => new WorkFileResourceContract()
			{
				FileName = t.FileName,
				FileUrl = t.FileUrl,
				FileExt = t.FileExt,
				FileType = t.FileType,
				FileDesc = t.FileDesc
			}).ToList();

			return new WorkService().CreateWorkFilePackage(workFilePackage);
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
			#region 参数验证与赋值
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
			#endregion

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
		/// 提交快传(附件)作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public dynamic submit_filework()
		{
			#region 参数验证与赋值
			var content = Request.GetBodyContent();

			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException("未接收到post数据", 1);
			}
			FileWorkDTO fileWorkAnswer;
			try
			{
				fileWorkAnswer = JSONHelper.Decode<FileWorkDTO>(content);
			}
			catch (Exception e)
			{
				return new ApiException(RetEum.ApplicationError, 2, "序列化失败,message:" + e.Message);
			}

			if (!fileWorkAnswer.WorkAnswers.Any())
			{
				return new ApiException(RetEum.ApplicationError, 3, "答案中不包含任何附件");
			}
			var _workService = new WorkService();
			var doWorkBase = new WorkBaseService().GetDoWorkBase(fileWorkAnswer.DoWorkId);
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			if (null == doWorkBase || doWorkBase.SonWorkType != 23)
			{
				return new ApiException(RetEum.ApplicationError, 4, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiException(RetEum.ApplicationError, 5, "当前用户没有提交作业权限");
			}
			var workFilePackage = _workService.GetWorkFilePackage(fileWorkAnswer.RecordId);
			if (null == workFilePackage)
			{
				return new ApiException(RetEum.ApplicationError, 6, "未找到指定的作业资源");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workFilePackage.RecordId) == -1)
			{
				return new ApiException(RetEum.ApplicationError, 7, " 作业参数不匹配");
			}
			var fileWork = _workService.GetUserFileWork(fileWorkAnswer.DoWorkId) ?? new FileWorksContracts();
			if (fileWork.Status == 2 || fileWork.Status == 3)
			{
				return new ApiException(RetEum.ApplicationError, 8, "作业已经完成,不允许重复提交");
			}
			#endregion

			fileWork.WorkId = doWorkBase.WorkID;
			fileWork.DoWorkId = fileWorkAnswer.DoWorkId;
			fileWork.RecordId = fileWorkAnswer.RecordId;
			fileWork.SubmitUserId = doWorkBase.SubmitUserID;
			fileWork.SubmitUserName = doWorkBase.SubmitUserName;
			fileWork.WorkLong = fileWorkAnswer.WorkLong;
			fileWork.SubmitDate = DateTime.Now;
			fileWork.IsTimeOut = fileWork.SubmitDate > doWorkBase.EffectiveDate;
			fileWork.SubmitCount = 1;
			fileWork.FileCount = fileWorkAnswer.WorkAnswers.Count();
			fileWork.Status = 2;

			int sid = 0;
			var workAnser = fileWorkAnswer.WorkAnswers.Select(t => new FileAnswer()
			{
				Sid = sid++,
				FileName = t.FileName ?? string.Empty,
				FileUrl = t.FileUrl ?? string.Empty,
				FileExt = t.FileExt ?? string.Empty,
				FileType = t.FileType,
				Comment = string.Empty
			});

			var answerList = new List<WorkAnswerContract>();
			answerList.Add(new WorkAnswerContract()
			{
				VersionId = fileWork.RecordId,
				AnswerType = 3,
				AnswerContent = JSONHelper.Encode<IEnumerable<FileAnswer>>(workAnser),
				Assess = 4,
				ResourceType = ResourceModuleOptions.WorkFile.ToString()
			});

			return _workService.DoFileWork(fileWork, answerList);
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
					requirementContent = t.RequirementContent??string.Empty,
					actualScore = unitWork.ActualScore,
					workScore = unitWork.WorkScore,
					isTimeOut = unitWokrs.ContainsKey(t.ContentId) && (unitWork.Status == 2 || unitWork.Status == 3) ? unitWork.IsTimeOut : doWorkBase.EffectiveDate < DateTime.Now,
					comment = unitWork.Comment ?? string.Empty,
					commentType = unitWork.CommentType,
					workStatus = unitWork.Status
				};
			});
		}

		/// <summary>
		/// 获取子资源作业提交情况
		/// </summary>
		/// <param name="workId"></param>
		/// <param name="contentId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic unitworks(long workId, long contentId)
		{
			var unitWorks = new WorkService().GetUnitWorks(contentId, workId).Where(t => t.Status == 2 || t.Status == 3);
			return unitWorks.Select(t => new
			{
				doId = t.DoId,
				contentId = t.ContentId,
				recordId = t.RecordId,
				doworkId = t.DoWorkId,
				workId = t.WorkId,
				submitUserId = t.SubmitUserId,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.SubmitDate.Epoch(),
				workLong = t.WorkLong,
				actualScore = t.ActualScore,
				isTimeOut = t.IsTimeOut,
				submitCount = t.SubmitCount,
				status = t.Status
			});
		}

		/// <summary>
		/// 快传(附件)作业完成情况
		/// </summary>
		/// <param name="workId"></param>
		/// <param name="recordId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic fileworks(long workId, long recordId)
		{
			var list = new List<long>() { 1, 2 };
			return list.Select(t => new
			{
				doId = t,
				recordId = t,
				doworkId = t,
				workId = (long)561,
				submitUserId = 1,
				submitUserName = "yuliang" ?? string.Empty,
				submitDate = DateTime.Now.Epoch(),
				workLong = 100,
				actualScore = "A",
				isTimeOut = 1,
				submitCount = 1,
				status = 3,
				comment = "做的很好",
				commentType = 1,
				fileCount = 5
			});
		}

		/// <summary>
		/// 老师查看布置作业内容详情
		/// </summary>
		[HttpGet, BasicAuthentication]
		public dynamic work_resources(long recordId, long workId)
		{
			var _workService = new WorkService();
			var workResources = _workService.GetWorkResources(recordId);

			if (!workResources.Any())
			{
				return Enumerable.Empty<object>();
			}
			var workBase = new WorkBaseService().GetWorkBase(workId);

			if (null == workBase)
			{
				return new ApiArgumentException("参数workId错误,未找到指定的作业");
			}

			var unitSummarys = _workService.GetUnitSummarys(recordId, workId).ToDictionary(c => c.ContentId, c => c);

			return workResources.Select(t => new
			{
				contentId = t.ContentId,
				packageId = t.PackageId,
				taskId = t.TaskId ?? string.Empty,
				moduleId = t.ModuleId,
				versionId = t.VersionId,
				relationPath = t.RelationPath ?? string.Empty,
				sonModuleId = t.SonModuleId ?? string.Empty,
				resourceName = t.ResourceName ?? string.Empty,
				resourceType = t.ResourceType ?? string.Empty,
				totalNum = workBase.TotalNum,
				completedNum = unitSummarys.ContainsKey(t.ContentId) ? unitSummarys[t.ContentId].CommentNum : 0,
				markNum = unitSummarys.ContainsKey(t.ContentId) ? unitSummarys[t.ContentId].MarkNum : 0
			});
		}

		/// <summary>
		/// 获取作业快传附件列表
		/// </summary>
		/// <param name="workId"></param>
		/// <param name="recordId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic workfiles(long recordId)
		{
			var list = new WorkService().GetWorkFileResources(recordId);

			return list.Select(t => new
			{
				versionId = t.ContentId,
				fileName = t.FileName ?? string.Empty,
				fileUrl = t.FileUrl ?? string.Empty,
				fileExt = t.FileExt ?? string.Empty,
				fileType = t.FileType,
				fileDesc = t.FileDesc ?? string.Empty
			});
		}

		/// <summary>
		/// 获取跟读语音答案
		/// </summary>
		/// <param name="workId"></param>
		/// <param name="contentId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic followread_answers(long workId, long contentId)
		{
			var unitWorkAnswers = new WorkService().GetUnitWorkAnswers(workId, contentId, true).GroupBy(t => t.DoId).ToDictionary(c => c.Key, c => c.ToList());

			return unitWorkAnswers.Select(t => new
			{
				doId = t.Key,
				userId = t.Value.First().SubmitUserId,
				answers = t.Value.Select(m => new
				{
					versionId = m.VersionId,
					assess = m.Assess,
					score = m.Score,
					answerContent = JSONHelper.Decode<List<ReadAnswerEntity>>(m.AnswerContent).Select(x => new
					{
						sid = x.Sid,
						word = x.Word,
						audioUrl = x.AudioUrl,
						readTimes = x.ReadTimes
					})
				})
			});
		}

		/// <summary>
		/// 批量点评作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public dynamic comment()
		{
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			long workId = Convert.ToInt64(request["workId"]);
			long contentId = Convert.ToInt64(request["contentId"]);
			int commentType = Convert.ToInt32(request["commentType"]);
			string userIds = request["userIds"] ?? string.Empty;
			string content = request["content"];
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var workBase = new WorkBaseService().GetWorkBase(workId);

			if (null == workBase)
			{
				return new ApiArgumentException("未找到指定的作业", 1);
			}
			if (workBase.PublishUserId != userId)
			{
				return new ApiResult() { Ret = RetEum.ApplicationError, ErrorCode = 2, Message = "暂无点评权限" };
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiResult() { Ret = RetEum.ApplicationError, ErrorCode = 3, Message = "点评内容不能为空" };
			}
			content = System.Web.HttpUtility.UrlDecode(content);
			if (content.Length > 300)
			{
				return new ApiArgumentException("点评内容不能超过300字", 5);
			}
			if (commentType != 1 && commentType != 2)
			{
				return new ApiArgumentException("参数commentType错误", 6);
			}
			var studentList = userIds.Split(',').Select(t => Convert.ToInt32(t));
			if (!studentList.Any())
			{
				return new ApiArgumentException("未找到指定的被点评用户", 7);
			}

			return new WorkService().CommentUnitWorks(studentList, workId, contentId, content, commentType);
		}

		/// <summary>
		/// 给未完成作业的孩子发提醒
		/// </summary>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic send_remind(long workId, long contentId)
		{
			var workBase = new WorkBaseProvider().GetWorkBase(workId);
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			if (null == workBase)
			{
				return new ApiArgumentException("未找到指定的作业", 1);
			}
			if (workBase.PublishUserID != userId)
			{
				return new ApiResult() { Ret = RetEum.ApplicationError, ErrorCode = 2, Message = "暂无提醒权限" };
			}
			
			var allUserList = new DoWorkBaseProvider().GetDoWorkList(workId).Select(t => t.SubmitUserID);
			var submitUserList = new WorkService().GetUnitWorks(workId, contentId).Where(t => t.Status == 2 || t.Status == 3).Select(t => t.SubmitUserId);
			var sendUserList = allUserList.Except(submitUserList).ToArray();

			if (!sendUserList.Any())
			{
				return new ApiResult() { Ret = RetEum.ApplicationError, ErrorCode = 3, Message = "所有人都已提交作业,无需提醒" };
			}

			var xiXinClient = new CApi.Client.XiXinClient();
			xiXinClient.Send(userId, sendUserList, "老师还没收到你的作业呢？前往我的书房赶紧写作业吧！" + workBase.WorkName);
			xiXinClient.SendToFamily(userId, sendUserList, "您的孩子还有作业未完成，提醒孩子前往我的书房赶紧交作业吧！" + workBase.WorkName);

			return true;
		}

		/// <summary>
		/// 获取第一级资源版本ID
		/// </summary>
		/// <param name="relationPath"></param>
		/// <returns></returns>
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
