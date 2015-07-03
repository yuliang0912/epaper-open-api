using CiWong.Framework.Helper;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.OpenAPI.Work.Service;
using CiWong.Resource.Preview.DataContracts;
using CiWong.Resource.Preview.Service;
using CiWong.Users;
using CiWong.Work.Contract;
using CiWong.Work.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.Work.Controllers
{
	public class WorkController : ApiController
	{
		private IWorkBase _workBase;
		private IDoWorkBase _doWorkBase;
		private WorkPublisher _workPublisher;
		private WorkService _workService;

		public WorkController(IWorkBase _workBase, IDoWorkBase _doWorkBase, WorkPublisher _workPublisher, WorkService _workService)
		{
			this._workBase = _workBase;
			this._doWorkBase = _doWorkBase;
			this._workPublisher = _workPublisher;
			this._workService = _workService;
		}

		/// <summary>
		/// 布置作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public dynamic publish()
		{
			#region 参数赋值及验证
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			long workPackageRecordId = Convert.ToInt64(request["recordId"]);
			string workName = request["workName"] ?? string.Empty;
			string workDesc = request["workDesc"] ?? string.Empty;
			int workDescType = Convert.ToInt32(request["workDescType"] ?? "1");//作业留言类型1:文本 2:语音(url地址)
			int completeDate = Convert.ToInt32(request["completeDate"]);
			int workType = Convert.ToInt32(request["workType"]);
			int sonWorkType = Convert.ToInt32(request["sonWorkType"]);
			int curriculum = Convert.ToInt32(request["curriculum"]);
			int publishType = Convert.ToInt32(request["publishType"]);
			string receiveObject = request["receiveObject"] ?? string.Empty;
			int sourceType = Convert.ToInt32(request["from"] ?? "0");
			var sendDate = DateTime.Now;

			if (!string.IsNullOrEmpty(request["sendDate"]))
			{
				sendDate = DateTimeExtensions.FromEpoch(Convert.ToInt32(request["sendDate"]));
			}

			if (!Enum.IsDefined(typeof(DictHelper.WorkTypeEnum), workType))
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4701, "参数workType不在指定的范围之内");
			}
			if (sonWorkType < 0 || sonWorkType >= 100)
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4702, "参数sonWorkType不在指定的范围之内");
			}
			if (!Enum.IsDefined(typeof(DictHelper.CurriculumEnum), curriculum))
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4703, "参数curriculum不在指定的范围之内");
			}
			if (!Enum.IsDefined(typeof(PublishTypeEnum), (byte)publishType))
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4704, "参数publishType不在指定的范围之内");
			}
			if (string.IsNullOrWhiteSpace(workName))
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4705, "参数workName不能为空");
			}
			if (completeDate < DateTime.Now.Epoch())
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4706, "作业截止时间不能小于当前时间");
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var receiveObjects = new List<KeyValuePair<long, string>>();
			try
			{
				receiveObjects = JSONHelper.Decode<List<KeyValuePair<long, string>>>(receiveObject);
			}
			catch (Exception e)
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4707, "序列化失败,Message:" + e.ToString());
			}
			var userInfo = new UserManager().GetUserInfo(userId);

			var workBase = new WorkBase()
			{
				WorkName = System.Web.HttpUtility.UrlDecode(workName),
				WorkType = (DictHelper.WorkTypeEnum)workType,
				SonWorkType = sonWorkType,
				PublishUserID = userId,
				PublishUserName = userInfo.RealName,
				PublishDate = DateTime.Now,
				SendDate = sendDate,
				EffectiveDate = DateTimeExtensions.FromEpoch(completeDate),
				IsSubmit = true,
				ViewStatus = ViewStatus.AllUsers,
				CompletedType = CompletedType.ReferLong,
				ReferLong = 30,
				CurriculumID = (DictHelper.CurriculumEnum)curriculum,
				PublishType = (PublishTypeEnum)publishType,
				WorkScore = 100,
				MarkType = MarkType.Auto,
				WorkDesc = System.Web.HttpUtility.UrlDecode(workDesc),
				RedirectParm = string.Format("bid_{0}.sid_{1}.zuop_{2}", workPackageRecordId, 0, 0),
				SourceType = sourceType,
				WorkDescType = workDescType
			};
			#endregion

			return _workPublisher.Publish(workBase, receiveObjects, null);
		}

		/// <summary>
		/// 获取作业基础信息
		/// </summary>
		/// <param name="workId"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic work_info(long workId)
		{
			var workBase = _workBase.GetWorkBase(workId);

			if (null == workBase)
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4708, "未找到指定的作业");
			}

			return new
			{
				workId = workBase.WorkID,
				workName = workBase.WorkName ?? string.Empty,
				workType = workBase.WorkType,
				sonWorkType = ConvertSonWorkType((int)workBase.WorkType, workBase.SonWorkType),
				publishUserId = workBase.PublishUserID,
				publishUserName = workBase.PublishUserName ?? string.Empty,
				sendDate = workBase.SendDate,
				effectiveDate = workBase.EffectiveDate,
				totalNum = workBase.TotalNum,
				completedNum = workBase.CompletedNum,
				markNum = workBase.MarkNum,
				reviceId = workBase.ReviceUserID,
				reviceName = workBase.ReviceUserName,
				recordId = _workPublisher.redirectParmsArray(workBase.RedirectParm),
				isTimeout = DateTime.Now > workBase.EffectiveDate,
				workDesc = workBase.WorkDesc
			};
		}

		/// <summary>
		///  删除已布置的作业
		/// </summary>
		/// <param name="workId"></param>
		/// <returns>0:失败 1:成功</returns>
		[HttpGet, BasicAuthentication]
		public dynamic delete_work(long workId)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			return _workBase.DeleteWorkBase(workId, userId);
		}

		/// <summary>
		/// 获取做作业列表(我的作业)
		/// </summary>
		/// <param name="workStatus">作业状态: 待完成0, 已完成7, 已批阅:3</param>
		/// <param name="from">请求来源: 1:安卓 2:IOS 3:阳光英语服务频道</param>
		[HttpGet, BasicAuthentication]
		public dynamic my_doworks(int workStatus, int from, int userId = 0, int sonWorkType = -1, int versionId = 1, int page = 1, int pageSize = 10)
		{
			userId = userId == 0 ? Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name) : userId;

			var sonWorkTypes = new List<int>();

			if (sonWorkType > 0)
			{
				sonWorkTypes.Add(sonWorkType);
			}
			else if (from == 1 || (from == 2 && versionId >= 2))
			{
				sonWorkTypes = new List<int>() { 11, 17, 18, 19, 23 };
			}
			else if (from == 2 && versionId == 1)
			{
				sonWorkTypes = new List<int>() { 23 };
			}
			else if (from == 3)
			{
				sonWorkTypes = new List<int>() { 17, 24 };
			}
			int totalItem = 0;

			var list = _doWorkBase.GetDoWorkListForApi(new List<int>() { userId }, null, sonWorkTypes, DateTime.MinValue, DateTime.Now, workStatus, -1, ref totalItem, page, pageSize);

			var recordIdList = list.Where(t => t.WorkType == DictHelper.WorkTypeEnum.电子书 || t.WorkType == DictHelper.WorkTypeEnum.电子报).Select(t => Convert.ToInt64(_workPublisher.redirectParmsArray(t.RedirectParm))).ToList();

			var recordList = _workService.GetWorkResources(recordIdList).GroupBy(t => t.RecordId).ToDictionary(c => c.Key, c => c.ToList());

			var userUnitWorks = _workService.GetUserUnitWorks(userId, recordIdList).Where(t => t.Status == 2 || t.Status == 3).ToDictionary(c => string.Concat(c.ContentId, "_", c.DoWorkId), c => c);

			var workBaseList = _workBase.GetWorkBaseList(list.Select(t => t.WorkID).Distinct(), -1).ToDictionary(c => c.WorkID, c => c);

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Where(t => workBaseList.Keys.Contains(t.WorkID)).Select(t =>
				{
					var currWorkBase = workBaseList[t.WorkID];
					var workPackageRecordId = Convert.ToInt64(_workPublisher.redirectParmsArray(t.RedirectParm));

					return new
					{
						workId = t.WorkID,
						doworkId = t.DoWorkID,
						workName = t.WorkName,
						workType = t.WorkType,
						sonWorkType = ConvertSonWorkType((int)t.WorkType, t.SonWorkType),
						publishUserId = currWorkBase.PublishUserID,
						publishUserName = currWorkBase.PublishUserName ?? string.Empty,
						sendDate = currWorkBase.SendDate,
						publishType = currWorkBase.PublishType,
						reviceId = currWorkBase.ReviceUserID,
						reviceName = currWorkBase.ReviceUserName ?? string.Empty,
						totalNum = currWorkBase.TotalNum,
						completedNum = currWorkBase.CompletedNum,
						markNum = currWorkBase.MarkNum,
						workDesc = currWorkBase.WorkDesc ?? string.Empty,
						submitUserId = t.SubmitUserID,
						submitUserName = t.SubmitUserName ?? string.Empty,
						submitDate = currWorkBase.PublishDate,
						workStatus = t.WorkStatus,
						workLong = t.WorkLong,
						actualScore = t.ActualScore,
						workScore = t.WorkScore,
						effectiveDate = t.EffectiveDate,
						isTimeout = t.EffectiveDate < DateTime.Now,
						recordId = workPackageRecordId,
						workResources = recordList.ContainsKey(workPackageRecordId) ? recordList[workPackageRecordId].Select(x =>
						{
							var unitWork = userUnitWorks.ContainsKey(string.Concat(x.ContentId, "_", t.DoWorkID)) ? userUnitWorks[string.Concat(x.ContentId, "_", t.DoWorkID)] : new UnitWorksContract();
							return new
							{
								contentId = x.ContentId,
								packageId = x.PackageId,
								taskId = x.TaskId ?? string.Empty,
								moduleId = x.ModuleId,
								versionId = x.VersionId,
								parentVersionId = GetRootVersionId(x.RelationPath),
								sonModuleId = x.SonModuleId ?? string.Empty,
								resourceName = x.ResourceName ?? string.Empty,
								resourceType = x.ResourceType ?? string.Empty,
								requirementContent = x.RequirementContent,
								isFull = x.IsFull,
								doId = unitWork.DoId,
								actualScore = unitWork.ActualScore,
								workStatus = unitWork.Status
							};
						}) : Enumerable.Empty<object>()
					};
				}).ToList()
			};
		}

		/// <summary>
		/// 老师获取自己布置的作业列表
		/// </summary>
		/// <param name="workStatus">作业状态:-1:全部 1:未提交 8:已提交 9:待完成  10:过期</param>
		/// <param name="sonWorkType">子作业类型 -1 全部</param>
		/// <param name="publishType">布置对象 -1:全部 0:个人布置 1班级布置 2孩子 4：班级小组</param>
		/// <param name="from">请求来源: 1:安卓 2:IOS 3:阳光英语服务频道 </param>
		/// <param name="reviceId">接受对象ID(当选择班级时,此处为班级ID)</param>
		[HttpGet, BasicAuthentication]
		public dynamic my_publishs(int workStatus, int sonWorkType, int publishType, int from, int versionId = 1, long reviceId = 0, int page = 1, int pageSize = 10)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var publishTypes = new List<PublishTypeEnum>();
			if (publishType > -1 && Enum.IsDefined(typeof(PublishTypeEnum), (byte)publishType))
			{
				publishTypes.Add((PublishTypeEnum)publishType);
			}
			var sonWorkTypes = new List<int>();

			if (sonWorkType > 0)
			{
				sonWorkTypes.Add(sonWorkType);
			}
			else if (from == 1)//安卓客户端支持的类型
			{
				sonWorkTypes = new List<int>() { 11, 17, 18, 19, 23 };
			}
			else if (from == 2 && versionId == 1)//IOS客户端支持的类型
			{
				sonWorkTypes = new List<int>() { 23 };
			}
			else if (from == 2 && versionId == 2)//IOS客户端支持的类型
			{
				sonWorkTypes = new List<int>() { 11, 17, 18, 19, 23 };
			}
			else if (from == 3)//电子报服务专区支持的类型
			{
				sonWorkTypes = new List<int>() { 17, 24 };
			}

			int totalItem = 0;
			var workBaseList = _workBase.GetWorkBaseListForApi(new List<int>() { userId }, publishTypes, sonWorkTypes, reviceId, DateTime.MinValue, DateTime.Now, workStatus, -1, ref totalItem, page, pageSize);

			var doWorkBaseList = new Dictionary<long, List<DoWorkBase>>();
			if ((from == 1 || from == 2) && workBaseList.Any())
			{
				if (workStatus == 1)
				{
					doWorkBaseList = _doWorkBase.GetDoWorkList(workBaseList.Select(t => t.WorkID), new List<int>() { 0, 1, 4 }).GroupBy(t => t.WorkID).ToDictionary(c => c.Key, c => c.OrderByDescending(m => m.SubmitDate).ToList());
				}
				else if (workStatus == 8)
				{
					doWorkBaseList = _doWorkBase.GetDoWorkList(workBaseList.Select(t => t.WorkID), new List<int>() { 2, 3, 5 }).GroupBy(t => t.WorkID).ToDictionary(c => c.Key, c => c.OrderByDescending(m => m.SubmitDate).ToList());
				}
			}

			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = workBaseList.Select(t => new
				{
					workId = t.WorkID,
					workName = t.WorkName ?? string.Empty,
					workType = t.WorkType,
					sonWorkType = ConvertSonWorkType((int)t.WorkType, t.SonWorkType),
					publishUserId = t.PublishUserID,
					publishUserName = t.PublishUserName ?? string.Empty,
					sendDate = t.SendDate,
					effectiveDate = t.EffectiveDate,
					totalNum = t.TotalNum,
					completedNum = t.CompletedNum,
					markNum = t.MarkNum,
					reviceId = t.ReviceUserID,
					reviceName = t.ReviceUserName,
					recordId = _workPublisher.redirectParmsArray(t.RedirectParm),//此处是作业资源包记录ID
					isTimeout = DateTime.Now > t.EffectiveDate,
					workDesc = t.WorkDesc,
					userList = doWorkBaseList.ContainsKey(t.WorkID) ? doWorkBaseList[t.WorkID].Select(x => new
					{
						submitUserId = x.SubmitUserID,
						submitUserName = x.SubmitUserName,
						workStatus = x.WorkStatus
					}) : Enumerable.Empty<object>()
				})
			};
		}

		/// <summary>
		/// 获取作业详细信息
		/// </summary>
		/// <param name="doworkId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic do_work_info(long workId, int userId = 0)
		{
			userId = userId == 0 ? Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name) : userId;

			var doWorkInfo = _doWorkBase.GetDoWorkBase(workId, userId);
			var workBase = _workBase.GetWorkBase(workId);

			if (null == doWorkInfo || null == workBase)
			{
				return new ApiArgumentException(ErrorCodeEum.Work_4708, "未找到指定的作业");
			}

			var workPackageRecordId = Convert.ToInt64(_workPublisher.redirectParmsArray(workBase.RedirectParm));

			var recordIdList = workBase.WorkType == DictHelper.WorkTypeEnum.电子报 || workBase.WorkType == DictHelper.WorkTypeEnum.电子书 ? new List<long>() { workPackageRecordId } : Enumerable.Empty<long>();

			var workPackageRecord = _workService.GetWorkResources(recordIdList);

			var userUnitWorks = _workService.GetUserUnitWorks(userId, recordIdList).Where(t => t.Status == 2 || t.Status == 3).ToDictionary(c => string.Concat(c.ContentId, "_", c.DoWorkId), c => c);

			return new
			{
				workId = doWorkInfo.WorkID,
				doworkId = doWorkInfo.DoWorkID,
				workName = doWorkInfo.WorkName,
				workType = doWorkInfo.WorkType,
				sonWorkType = ConvertSonWorkType((int)doWorkInfo.WorkType, doWorkInfo.SonWorkType),
				publishUserId = workBase.PublishUserID,
				publishUserName = workBase.PublishUserName ?? string.Empty,
				sendDate = workBase.SendDate,
				publishType = workBase.PublishType,
				reviceId = workBase.ReviceUserID,
				reviceName = workBase.ReviceUserName ?? string.Empty,
				totalNum = workBase.TotalNum,
				completedNum = workBase.CompletedNum,
				markNum = workBase.MarkNum,
				workDesc = workBase.WorkDesc ?? string.Empty,
				submitUserId = doWorkInfo.SubmitUserID,
				submitUserName = doWorkInfo.SubmitUserName ?? string.Empty,
				submitDate = workBase.PublishDate,
				workStatus = doWorkInfo.WorkStatus,
				workLong = doWorkInfo.WorkLong,
				actualScore = doWorkInfo.ActualScore,
				workScore = doWorkInfo.WorkScore,
				effectiveDate = doWorkInfo.EffectiveDate,
				isTimeout = doWorkInfo.EffectiveDate < DateTime.Now,
				recordId = workPackageRecordId,
				workResources = workPackageRecord.Select(x =>
				{
					var unitWork = userUnitWorks.ContainsKey(string.Concat(x.ContentId, "_", doWorkInfo.DoWorkID)) ? userUnitWorks[string.Concat(x.ContentId, "_", doWorkInfo.DoWorkID)] : new UnitWorksContract();
					return new
					{
						contentId = x.ContentId,
						packageId = x.PackageId,
						taskId = x.TaskId ?? string.Empty,
						moduleId = x.ModuleId,
						versionId = x.VersionId,
						relationPath = x.RelationPath ?? string.Empty,
						sonModuleId = x.SonModuleId ?? string.Empty,
						resourceName = x.ResourceName ?? string.Empty,
						resourceType = x.ResourceType ?? string.Empty,
						requirementContent = x.RequirementContent,
						isFull = x.IsFull,
						doId = unitWork.DoId,
						actualScore = unitWork.ActualScore,
						workStatus = unitWork.Status
					};
				})
			};
		}

		/// <summary>
		/// 获取该份作业的所有学生列表
		/// </summary>
		[HttpGet]
		public dynamic doworks(long workId)
		{
			var doWorkList = _doWorkBase.GetDoWorkList(workId);

			return doWorkList.Select(t => new
			{
				workId = t.WorkID,
				doworkId = t.DoWorkID,
				workName = t.WorkName ?? string.Empty,
				submitUserId = t.SubmitUserID,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.SubmitDate,
				workStatus = t.WorkStatus,
				workLong = t.WorkLong,
				actualScore = t.ActualScore,
				workScore = t.WorkScore
			});
		}

		/// <summary>
		/// 判断资源是否布置过作业
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="cid"></param>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic last_publishs(long packageId, string cid, string moduleIds = "")
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var moduleIdList = new List<int>();

			if (!string.IsNullOrWhiteSpace(moduleIds))
			{
				moduleIdList = moduleIds.Split(',').Select(t => Convert.ToInt32(t)).ToList();
			}

			var redirectParms = _workService.GetPublishRecord(new List<int>() { userId }, moduleIdList, packageId, cid, 5).Select(t => string.Format("bid_{0}.sid_0.zuop_0", t.RecordId));

			if (null == redirectParms || !redirectParms.Any())
			{
				return Enumerable.Empty<WorkBase>();
			}

			var workBaseList = _workBase.GetLastPublish(userId, new List<int>() { 101, 102 }, redirectParms, 3);

			return workBaseList.Select(t => new
			{
				workId = t.WorkID,
				workName = t.WorkName ?? string.Empty,
				workType = t.WorkType,
				sonWorkType = ConvertSonWorkType((int)t.WorkType, t.SonWorkType),
				publishUserId = t.PublishUserID,
				publishUserName = t.PublishUserName ?? string.Empty,
				sendDate = t.SendDate,
				effectiveDate = t.EffectiveDate,
				totalNum = t.TotalNum,
				completedNum = t.CompletedNum,
				markNum = t.MarkNum,
				reviceId = t.ReviceUserID,
				reviceName = t.ReviceUserName,
				recordId = _workPublisher.redirectParmsArray(t.RedirectParm),//此处是作业资源包记录ID
				isTimeout = DateTime.Now > t.EffectiveDate,
				workDesc = t.WorkDesc
			});
		}

		/// <summary>
		/// 获取最后接受的作业
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="cid"></param>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		[HttpGet, BasicAuthentication]
		public dynamic last_doworks(long packageId, string cid, string moduleIds = "")
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var classList = CiWong.Relation.WCFProxy.ClassRelationProxy.GetClassByUserId(userId).Take(15);

			var teacherList = new List<int>();

			foreach (var item in classList)
			{
				teacherList.AddRange(CiWong.Relation.WCFProxy.ClassRelationProxy.GetTeacher(item.ID).Select(t => t.RoomUserID).ToList());
			}

			teacherList = teacherList.Distinct().ToList();

			var moduleIdList = new List<int>();
			if (!string.IsNullOrWhiteSpace(moduleIds))
			{
				moduleIdList = moduleIds.Split(',').Select(t => Convert.ToInt32(t)).ToList();
			}

			var redirectParms = _workService.GetPublishRecord(teacherList, moduleIdList, packageId, cid, 5).Select(t => string.Format("bid_{0}.sid_0.zuop_0", t.RecordId));

			if (null == redirectParms || !redirectParms.Any())
			{
				return Enumerable.Empty<DoWorkBase>();
			}

			var doWorkList = _doWorkBase.GetLastWorks(userId, new List<int>() { 101, 102 }, redirectParms, 3);

			return doWorkList.Select(t => new
			{
				workId = t.WorkID,
				doworkId = t.DoWorkID,
				workName = t.WorkName,
				workType = t.WorkType,
				sonWorkType = ConvertSonWorkType((int)t.WorkType, t.SonWorkType),
				publishUserId = t.WorkBase.PublishUserID,
				publishUserName = t.WorkBase.PublishUserName ?? string.Empty,
				sendDate = t.WorkBase.SendDate,
				publishType = t.WorkBase.PublishType,
				reviceId = t.WorkBase.ReviceUserID,
				reviceName = t.WorkBase.ReviceUserName ?? string.Empty,
				totalNum = t.WorkBase.TotalNum,
				completedNum = t.WorkBase.CompletedNum,
				markNum = t.WorkBase.MarkNum,
				workDesc = t.WorkDesc ?? string.Empty,
				submitUserId = t.SubmitUserID,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.WorkBase.PublishDate,
				workStatus = t.WorkStatus,
				workLong = t.WorkLong,
				actualScore = t.ActualScore,
				workScore = t.WorkScore,
				effectiveDate = t.EffectiveDate,
				isTimeout = t.EffectiveDate < DateTime.Now,
				recordId = _workPublisher.redirectParmsArray(t.RedirectParm)
			});
		}

		/// <summary>
		/// 转换作业类型,支持安卓端
		/// </summary>
		/// <returns></returns>
		private int ConvertSonWorkType(int workType, int sonWorkType)
		{
			if (workType == 101 && sonWorkType == 11)
			{
				return 17;
			}
			else if (workType == 102 && sonWorkType == 11)
			{
				return 18;
			}
			else
			{
				return sonWorkType;
			}
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
