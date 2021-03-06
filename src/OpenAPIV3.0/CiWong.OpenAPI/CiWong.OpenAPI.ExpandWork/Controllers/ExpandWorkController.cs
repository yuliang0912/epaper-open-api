﻿using CiWong.Framework.Helper;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.OpenAPI.ExpandWork.DTO;
using CiWong.Relation.WCFProxy;
using CiWong.Resource.Preview.DataContracts;
using CiWong.Resource.Preview.Service;
using CiWong.Tools.Package.Services;
using CiWong.Users;
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
		private WorkService _workService;
		private PackageService _packageService;
		private WorkBaseService _workBaseService;
		public ExpandWorkController(WorkService _workService, WorkBaseService _workBaseService, PackageService _packageService)
		{
			this._workService = _workService;
			this._packageService = _packageService;
			this._workBaseService = _workBaseService;
		}

		/// <summary>
		/// 获取作业资源包中的资源所属书籍信息
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic work_package_info(long recordId)
		{
			var publishRecord = _workService.GetWorkPackage(recordId);
			if (null == publishRecord)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4801, "未找到指定的作业记录");
			}

			var package = _packageService.GetPackageForApi(publishRecord.PackageId);
			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4802, "未找到指定的资源包");
			}

			return new
			{
				appId = publishRecord.AppId,
				productId = publishRecord.ProductId,
				productName = publishRecord.PackageName,
				cover = package.Cover,
				packageId = publishRecord.PackageId,
				price = package.Price
			};
		}

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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4803, "参数workType不在指定的范围内");
			}
			if (string.IsNullOrWhiteSpace(packageInfo))
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			WorkPackageDTO workPackage = null;
			try
			{
				workPackage = JSONHelper.Decode<WorkPackageDTO>(System.Web.HttpUtility.UrlDecode(packageInfo));
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化失败,message:" + e.Message);
			}
			if (!workPackage.workResources.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4806, "作业资源包中不包含任何资源");
			}

			var package = _packageService.GetPackageForApi(workPackage.PackageId);
			if (null == package)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4802, "未找到指定的资源包");
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new UserManager().GetUserInfo(userId);

			var publishRecord = new PublishRecordContract()
			{
				AppId = workPackage.AppId,
				ProductId = workPackage.ProductId.ToString(),
				PackageId = workPackage.PackageId,
				PackageName = package.BookName,
				PackageType = package.GroupType,
				UserId = userId,
				UserName = userInfo.RealName,
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
			}).ToList(); //此处2个ToList不可以省略

			var recordId = _workService.CreateWorkPackage(publishRecord);
			if (recordId < 1)
			{
				return new ApiResult()
				{
					Ret = RetEum.Success,
					ErrorCode = ErrorCodeEum.ExpandWork_4807,
					Message = "创建作业资源包失败"
				};
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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			List<WorkFileDTO> workFiles = new List<WorkFileDTO>();
			try
			{
				workFiles = JSONHelper.Decode<List<WorkFileDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化失败,message:" + e.Message);
			}
			if (!workFiles.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4808, "集合中不包含任何元素");
			}
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new UserManager().GetUserInfo(userId);
			var workFilePackage = new WorkFilePackageContract();
			workFilePackage.FilePackageName = "作业快传附件资源包";
			workFilePackage.FilePackageType = 2;
			workFilePackage.UserId = userId;
			workFilePackage.UserName = userInfo.RealName;
			workFilePackage.CreateDate = DateTime.Now;

			workFilePackage.WorkFileResources = workFiles.Select(t => new WorkFileResourceContract()
			{
				FileName = t.FileName ?? string.Empty,
				FileUrl = t.FileUrl ?? string.Empty,
				FileExt = t.FileExt ?? string.Empty,
				FileType = t.FileType,
				FileDesc = t.FileDesc ?? string.Empty
			}).ToList();

			return _workService.CreateWorkFilePackage(workFilePackage);
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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			UnitWorkDTO<FollowReadAnswerDTO> unitWorkAnswer;
			try
			{
				unitWorkAnswer = JSONHelper.Decode<UnitWorkDTO<FollowReadAnswerDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化失败,message:" + e.Message);
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var doWorkBase = _workBaseService.GetDoWorkBase(unitWorkAnswer.DoWorkId);
			if (null == doWorkBase)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4810, "当前用户没有提交作业权限");
			}
			var workResource = _workService.GetWorkResource(unitWorkAnswer.ContentId);
			if (null == workResource)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4811, "未找到指定的作业资源");
			}
			if (workResource.ModuleId != 10)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4812, "错误的资源模块类型");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workResource.RecordId) == -1)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4813, "作业参数不匹配");
			}

			var unitWork = _workService.GetUserUnitWork(unitWorkAnswer.ContentId, unitWorkAnswer.DoWorkId) ?? new UnitWorksContract();

			if (unitWork.Status == 2 || unitWork.Status == 3)
			{
				return new ApiException(errCode: ErrorCodeEum.ExpandWork_4814, message: "作业已经完成,不允许重复提交");
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
				return new ApiResult()
				{
					Ret = RetEum.Success,
					ErrorCode = ErrorCodeEum.ExpandWork_4815,
					Message = "作业提交失败"
				};
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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			UnitWorkDTO<ListenSpeakAnswerDTO> unitWorkAnswer;
			try
			{
				unitWorkAnswer = JSONHelper.Decode<UnitWorkDTO<ListenSpeakAnswerDTO>>(content);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化容失败,message:" + e.Message);
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var doWorkBase = _workBaseService.GetDoWorkBase(unitWorkAnswer.DoWorkId);
			if (null == doWorkBase)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4810, "当前用户没有提交作业权限");
			}
			var workResource = _workService.GetWorkResource(unitWorkAnswer.ContentId);
			if (null == workResource)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4811, "未找到指定的作业资源");
			}
			if (workResource.ModuleId != 15)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4812, "错误的资源类型");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workResource.RecordId) == -1)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4813, "作业参数不匹配");
			}
			var unitWork = _workService.GetUserUnitWork(unitWorkAnswer.ContentId, unitWorkAnswer.DoWorkId) ?? new UnitWorksContract();

			if (unitWork.Status == 2 || unitWork.Status == 3)
			{
				return new ApiException(errCode: ErrorCodeEum.ExpandWork_4814, message: "作业已经完成,不允许重复提交");
			}
			#endregion

			unitWork.ContentId = workResource.ContentId;
			unitWork.RecordId = workResource.RecordId;
			unitWork.WorkId = doWorkBase.WorkID;
			unitWork.DoWorkId = doWorkBase.DoWorkID;
			unitWork.SubmitUserId = doWorkBase.SubmitUserID;
			unitWork.SubmitUserName = doWorkBase.SubmitUserName;
			unitWork.ActualScore = Math.Round(unitWorkAnswer.WorkAnswers.Sum(t => t.Score));
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
					OptionId = x.OptionId ?? string.Empty,
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
				return new ApiResult()
				{
					Ret = RetEum.Success,
					ErrorCode = ErrorCodeEum.ExpandWork_4815,
					Message = "作业提交失败"
				};
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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			FileWorkDTO fileWorkAnswer;
			try
			{
				fileWorkAnswer = JSONHelper.Decode<FileWorkDTO>(content);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化容失败,message:" + e.Message);
			}

			if (!fileWorkAnswer.WorkAnswers.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4816, "答案中不包含任何附件");
			}

			var doWorkBase = _workBaseService.GetDoWorkBase(fileWorkAnswer.DoWorkId);
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			if (null == doWorkBase || doWorkBase.SonWorkType != 23)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (doWorkBase.SubmitUserID != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4810, "当前用户没有提交作业权限");
			}
			var workFilePackage = _workService.GetWorkFilePackage(fileWorkAnswer.RecordId);
			if (null == workFilePackage)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4817, "未找到指定的附件作业资源");
			}
			if (doWorkBase.RedirectParm.IndexOf("bid_" + workFilePackage.RecordId) == -1)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4813, "作业参数不匹配");
			}
			var fileWork = _workService.GetUserFileWork(fileWorkAnswer.DoWorkId) ?? new FileWorksContracts();
			if (fileWork.Status == 2 || fileWork.Status == 3)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4814, "作业已经完成,不允许重复提交");
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
			fileWork.FileCount = fileWorkAnswer.WorkAnswers.Count();
			fileWork.Message = fileWorkAnswer.Message;
			fileWork.SubmitCount = 1;
			fileWork.Status = 2;
			fileWork.WorkLevel = -1;

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
			var workResources = _workService.GetWorkResources(recordId);
			if (!workResources.Any())
			{
				return Enumerable.Empty<object>();
			}
			var doWorkBase = _workBaseService.GetDoWorkBase(doWorkId);
			if (null == doWorkBase)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
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
					requirementContent = t.RequirementContent ?? string.Empty,
					isFull = t.IsFull,
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
			var unitWorks = _workService.GetUnitWorks(contentId, workId).Where(t => t.Status == 2 || t.Status == 3);

			return unitWorks.OrderByDescending(t => t.ActualScore).ThenBy(t => t.SubmitDate).Select(t => new
			{
				doId = t.DoId,
				contentId = t.ContentId,
				recordId = t.RecordId,
				doworkId = t.DoWorkId,
				workId = t.WorkId,
				submitUserId = t.SubmitUserId,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.SubmitDate,
				workLong = t.WorkLong,
				actualScore = t.ActualScore,
				isTimeOut = t.IsTimeOut,
				submitCount = t.SubmitCount,
				comment = t.Comment ?? string.Empty,
				commentType = t.CommentType,
				status = t.Status,
			}).ToList();
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
			var list = _workService.GetFileWorks(recordId, workId);

			var fileWorkAnswers = _workService.GetFileWorkAnswers(workId, recordId, true).ToDictionary(c => c.DoId, c => c);

			return list.Select(t => new
			{
				doId = t.DoId,
				workId = t.WorkId,
				doworkId = t.DoWorkId,
				recordId = t.RecordId,
				submitUserId = t.SubmitUserId,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.SubmitDate.Epoch(),
				workLong = t.WorkLong,
				workLevel = t.WorkLevel,
				isTimeOut = t.IsTimeOut,
				submitCount = t.SubmitCount,
				message = t.Message ?? string.Empty,
				comment = t.Comment ?? string.Empty,
				commentType = t.CommentType,
				status = t.Status,
				workAnswers = fileWorkAnswers.ContainsKey(t.DoId) ? JSONHelper.Decode<List<FileAnswer>>(fileWorkAnswers[t.DoId].AnswerContent).Select(m => new
				{
					sid = m.Sid,
					fileName = m.FileName ?? string.Empty,
					fileUrl = m.FileUrl,
					fileExt = m.FileExt,
					fileType = m.FileType,
					comment = m.Comment ?? string.Empty
				}) : Enumerable.Empty<object>()
			});
		}

		/// <summary>
		/// 老师查看布置作业内容详情
		/// </summary>
		[HttpGet]
		public dynamic work_resources(long recordId, long workId)
		{
			var workResources = _workService.GetWorkResources(recordId);
			if (!workResources.Any())
			{
				return Enumerable.Empty<object>();
			}
			var workBase = _workBaseService.GetWorkBase(workId);
			if (null == workBase)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (workBase.WorkType != 101 && workBase.WorkType != 102)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4818, "当前作业不在查找范围之内");
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
				completedNum = unitSummarys.ContainsKey(t.ContentId) ? unitSummarys[t.ContentId].CompletedNum : 0,
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
		public dynamic work_file_resources(string recordIds)
		{
			var recordIdList = recordIds.Split(',').Select(t => Convert.ToInt64(t));

			if (!recordIdList.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4819, "未能解析正确的记录ID");
			}

			var list = _workService.GetWorkFileResources(recordIdList).GroupBy(t => t.RecordId);

			return list.Select(t => new
			{
				recordId = t.Key,
				fileResources = t.Select(x => new
				{
					versionId = x.ContentId,
					fileName = x.FileName ?? string.Empty,
					fileUrl = x.FileUrl ?? string.Empty,
					fileExt = x.FileExt ?? string.Empty,
					fileType = x.FileType,
					fileDesc = x.FileDesc ?? string.Empty
				})
			});
		}

		/// <summary>
		/// 快传作业答案
		/// </summary>
		/// <param name="doId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic file_work_answers(long doWorkId)
		{
			var fileWork = _workService.GetUserFileWork(doWorkId);
			if (null == fileWork)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4820, "未找到指定的附件作业或者作业尚未完成");
			}

			//作业快传老师和家长需要查看作业
			//int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			//if (fileWork.SubmitUserId != userId)
			//{
			//	return new ApiArgumentException(ErrorCodeEum.ExpandWork_4821, "当前用户没有作业查看权限");
			//}

			var workAnswer = _workService.GetAnswer(fileWork.DoId, 3, fileWork.RecordId);

			return new
			{
				doId = fileWork.DoId,
				workId = fileWork.WorkId,
				doworkId = fileWork.DoWorkId,
				recordId = fileWork.RecordId,
				submitUserId = fileWork.SubmitUserId,
				submitUserName = fileWork.SubmitUserName ?? string.Empty,
				submitDate = fileWork.SubmitDate.Epoch(),
				workLong = fileWork.WorkLong,
				WorkLevel = fileWork.WorkLevel,
				isTimeOut = fileWork.IsTimeOut,
				submitCount = fileWork.SubmitCount,
				message = fileWork.Message ?? string.Empty,
				comment = fileWork.Comment ?? string.Empty,
				commentType = fileWork.CommentType,
				status = fileWork.Status,
				workAnswers = workAnswer != null && !string.IsNullOrWhiteSpace(workAnswer.AnswerContent) ? JSONHelper.Decode<List<FileAnswer>>(workAnswer.AnswerContent).Select(m => new
				{
					sid = m.Sid,
					fileName = m.FileName ?? string.Empty,
					fileUrl = m.FileUrl,
					fileExt = m.FileExt,
					fileType = m.FileType,
					comment = m.Comment ?? string.Empty
				}) : Enumerable.Empty<object>()
			};
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
			var unitWorkAnswers = _workService.GetUnitWorkAnswers(workId, contentId, true)
				 .GroupBy(t => t.DoId).ToDictionary(c => c.Key, c => c.ToList());

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

			var workBase = _workBaseService.GetWorkBase(workId);

			if (null == workBase || workBase.WorkType < 100)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (workBase.PublishUserId != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4822, "当前用户没有点评权限");
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4823, "点评内容不能为空");
			}
			content = System.Web.HttpUtility.UrlDecode(content);
			if (content.Length > 300)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4824, "点评内容不能超过300字");
			}
			if (commentType != 1 && commentType != 2)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4825, "参数commentType错误");
			}
			var studentList = userIds.Split(',').Select(t => Convert.ToInt32(t));
			if (!studentList.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4826, "未找到指定的被点评用户");
			}

			return _workService.CommentUnitWorks(studentList, workId, contentId, content, commentType);
		}

		/// <summary>
		/// 批量点评附件作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public dynamic comment_file_work()
		{
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			long workId = Convert.ToInt64(request["workId"]);
			long recordId = Convert.ToInt64(request["recordId"]);
			int commentType = Convert.ToInt32(request["commentType"]);
			string userIds = request["userIds"] ?? string.Empty;
			string content = request["content"] ?? string.Empty;
			decimal workLevel = Convert.ToDecimal(request["workLevel"] ?? "-1");
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var workBase = _workBaseService.GetWorkBase(workId);

			if (null == workBase || workBase.SonWorkType != 23)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (workBase.PublishUserId != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4822, "当前用户没有点评权限");
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4823, "点评内容不能为空");
			}
			content = System.Web.HttpUtility.UrlDecode(content);
			if (content.Length > 300)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4824, "点评内容不能超过300字");
			}
			if (commentType != 1 && commentType != 2)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4825, "参数commentType错误");
			}
			var studentList = userIds.Split(',').Select(t => Convert.ToInt32(t));
			if (!studentList.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4826, "未找到指定的被点评用户");
			}
			return _workService.CommentFileWorks(studentList, workId, recordId, workLevel, content, commentType);
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
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (workBase.PublishUserID != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4822, "当前用户没有点评权限");
			}

			var doWorkList = new DoWorkBaseProvider().GetDoWorkList(workId);

			var allUserList = doWorkList.Select(t => t.SubmitUserID).ToList();
			var sendUserList = new int[] { };
			if (workBase.WorkType == DictHelper.WorkTypeEnum.电子报 || workBase.WorkType == DictHelper.WorkTypeEnum.电子报)
			{
				var submitUserList = _workService.GetUnitWorks(contentId, workId).Where(t => t.Status == 2 || t.Status == 3).Select(t => t.SubmitUserId).ToList();
				sendUserList = allUserList.Except(submitUserList).ToArray();
			}
			else
			{
				sendUserList = doWorkList.Where(t => t.WorkStatus == Work.Entities.DoWorkStatusEnum.UnSubmit || t.WorkStatus == Work.Entities.DoWorkStatusEnum.Temporary || t.WorkStatus == Work.Entities.DoWorkStatusEnum.Back).Select(t => t.SubmitUserID).ToArray();
			}

			if (!sendUserList.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4827, "所有人都已提交作业,无需提醒");
			}

			var xiXinClient = new CApi.Client.XiXinClient();
			xiXinClient.Send(userId, sendUserList, "老师还没收到你的作业呢？前往我的书房赶紧写作业吧！" + workBase.WorkName);
			xiXinClient.SendToFamily(userId, sendUserList, "您的孩子还有作业未完成，提醒孩子前往我的书房赶紧交作业吧！" + workBase.WorkName);

			return true;
		}

		/// <summary>
		/// 涂鸦附件作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public dynamic graffiti_file_work()
		{
			var content = Request.GetBodyContent();

			if (string.IsNullOrWhiteSpace(content))
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4804, "post数据不能为空");
			}
			GraffitiFileWorkDTO graffitiFileWork = null;
			try
			{
				graffitiFileWork = JSONHelper.Decode<GraffitiFileWorkDTO>(content);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4805, "序列化失败,message:" + e.ToString());
			}
			if (!graffitiFileWork.GraffitiFiles.Any())
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4828, "数据中不包含任何涂鸦附件");
			}
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var doWorkBase = _workBaseService.GetDoWorkBase(graffitiFileWork.DoWorkId);

			if (null == doWorkBase || doWorkBase.WorkType != 103)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4809, "未找到指定的作业");
			}
			if (doWorkBase.PublishUserId != userId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4822, "当前用户没有点评权限");
			}

			var userFileWork = _workService.GetUserFileWork(graffitiFileWork.DoWorkId);

			if (doWorkBase.RedirectParm.IndexOf("bid_" + userFileWork.RecordId) == -1 || userFileWork.DoId != graffitiFileWork.DoId)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4813, "作业参数不匹配");
			}
			var userAnswer = _workService.GetAnswer(userFileWork.DoId, 3, userFileWork.RecordId);
			if (null == userAnswer)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4829, "未找到作业答案");
			}
			var userFileAnswers = JSONHelper.Decode<List<FileAnswer>>(userAnswer.AnswerContent);
			var graffitiFiles = graffitiFileWork.GraffitiFiles.ToDictionary(c => c.Sid, c => c.Comment);
			foreach (var item in userFileAnswers)
			{
				if (graffitiFiles.ContainsKey(item.Sid))
				{
					item.Comment = graffitiFiles[item.Sid];
				}
			}
			userAnswer.AnswerContent = JSONHelper.Encode<List<FileAnswer>>(userFileAnswers);

			return _workService.CorrectFileWorkAnswer(doWorkBase.WorkID, doWorkBase.DoWorkID, doWorkBase.SubmitUserID, userAnswer);
		}

		/// <summary>
		/// 获取筛选的跟读单词详情
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic part_followread_word_details(long contentId)
		{
			var workResource = _workService.GetWorkResource(contentId);

			if (null == workResource || workResource.ModuleId != 10)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4811, "未找到指定的作业资源");
			}

			var wordList = new CiWong.Tools.Workshop.DataContracts.ReturnResult<IEnumerable<CiWong.Tools.Workshop.DataContracts.WordContract>>();
			if (workResource.IsFull)
			{
				var resourceParts = _workService.GetResourceParts(contentId).Select(t => t.VersionId).ToArray();

				wordList = CiWong.Tools.Workshop.Services.ResourceServices.Instance.GetByVersionIds<CiWong.Tools.Workshop.DataContracts.WordContract>(resourceParts);
			}
			if (wordList.Data == null || !wordList.Data.Any())
			{
				var result = CiWong.Tools.Workshop.Services.ResourceServices.Instance.GetByVersionId<CiWong.Tools.Workshop.DataContracts.SyncFollowReadContract>(workResource.VersionId);

				if (result.Data == null)
				{
					return new ApiArgumentException(ErrorCodeEum.ExpandWork_4830, "参数versionId错误，未找到指定资源");
				}
				var wordVersionList = result.Data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word)
					.SelectMany(t => t.List).Where(t => t.VersionId.HasValue).Select(t => t.VersionId.Value).ToArray();

				wordList = CiWong.Tools.Workshop.Services.ResourceServices.Instance.GetByVersionIds<CiWong.Tools.Workshop.DataContracts.WordContract>(wordVersionList);
			}

			return wordList.Data.Select(x => new
			{
				wId = x.VersionId ?? 0,
				words = x.Name ?? string.Empty,
				wordFile = x.AudioUrl ?? string.Empty,
				wordType = x.IsExpand.HasValue ? x.IsExpand.Value : false,
				symbol = x.Symbol ?? string.Empty,
				syllable = x.Syllable ?? string.Empty,
				pretations = x.Pretations ?? string.Empty,
				sentences = x.Sentences.Any() ? x.Sentences.First().Text ?? string.Empty : string.Empty,
				sentFile = x.Sentences.Any() ? x.Sentences.First().AudioUrl ?? string.Empty : string.Empty,
				wordPic = x.PictureUrl ?? string.Empty
			});
		}

		/// <summary>
		/// 获取筛选的跟读句子详情
		/// </summary>
		/// <param name="contentId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic part_followread_text_sentences(long contentId)
		{
			var workResource = _workService.GetWorkResource(contentId);

			if (null == workResource || workResource.ModuleId != 10)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4811, "未找到指定的作业资源");
			}

			var resourceParts = new List<long>();

			if (workResource.IsFull)
			{
				resourceParts = _workService.GetResourceParts(contentId).Where(t => t.ResourceType == ResourceModuleOptions.Phrase.ToString()).Select(t => t.VersionId).ToList();
			}

			var result = CiWong.Tools.Workshop.Services.ResourceServices.Instance.GetByVersionId<CiWong.Tools.Workshop.DataContracts.SyncFollowReadTextContract>(workResource.VersionId);

			if (!result.IsSucceed || result.Data == null)
			{
				return new ApiArgumentException(ErrorCodeEum.ExpandWork_4830, "参数versionId错误，未找到指定资源");
			}

			var sentences = result.Data.Sections.SelectMany(t => t.Sentences).ToList();

			if (resourceParts.Any())
			{
				sentences = sentences.Where(t => t.VersionId.HasValue && resourceParts.Contains(t.VersionId.Value)).ToList();
			}

			return sentences.Select(x => new
			{
				content = x.Content ?? string.Empty,
				audioUrl = x.AudioUrl ?? string.Empty,
				versionId = x.VersionId ?? 0,
				resourceModuleId = x.ModuleId ?? Guid.Empty,
				name = x.Name ?? string.Empty
			});
		}


		/// <summary>
		/// 班级提交情况统计
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="beginData"></param>
		/// <param name="endData"></param>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic work_census(long classId, string beginDate, string endDate, int moduleId = 0)
		{
			DateTime _beginDate, _endDate;

			if (!DateTime.TryParse(beginDate, out _beginDate))
			{
				return new ApiArgumentException(ErrorCodeEum.ArgumentFormatError, "参数beginDate格式错误");
			}
			if (!DateTime.TryParse(endDate, out _endDate))
			{
				return new ApiArgumentException(ErrorCodeEum.ArgumentFormatError, "参数endData格式错误");
			}

			var studentList = ClassRelationProxy.GetClassStudentMember(classId);

			var workCensus = _workService.GetWorkCensus(studentList.Select(t => t.RoomUserID), _beginDate, _endDate, moduleId).OrderByDescending(t => t.TotalSubmitNum);

			return workCensus.Select(t => new
			{
				userId = t.UserId,
				userName = t.UserName,
				totalWorkNum = t.TotalWorkNum,
				totalSubmitNum = t.TotalSubmitNum,
				totalWorkLong = t.TotalWorkLong,
			});
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
