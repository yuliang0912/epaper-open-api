using CiWong.Framework.Helper;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.OpenAPI.Work.Service;
using CiWong.Users;
using CiWong.Work.Entities;
using CiWong.Work.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace CiWong.OpenAPI.Work.Controllers
{
	public class WorkController : ApiController
	{

		[HttpGet, BasicAuthentication]
		public dynamic GetUserInfo()
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new UserManager().GetUserInfo(userId);
			return userInfo.RealName;
		}
		/// <summary>
		/// 布置作业
		/// </summary>
		/// <returns></returns>
		[HttpPost, BasicAuthentication]
		public List<long> publish()
		{
			#region 参数赋值及验证
			var request = ((System.Web.HttpContextBase)Request.Properties["MS_HttpContext"]).Request;
			request.ContentEncoding = Encoding.UTF8;

			long workPackageRecordId = Convert.ToInt64(request["recordId"]);
			string workName = request["workName"] ?? string.Empty;
			string workDesc = request["workDesc"] ?? string.Empty;
			int workDescType = Convert.ToInt32(request["workDescType"]);//作业留言类型1:文本 2:语音(url地址)
			int completeDate = Convert.ToInt32(request["completeDate"]);
			int workType = Convert.ToInt32(request["workType"]);
			int sonWorkType = Convert.ToInt32(request["sonWorkType"]);
			int curriculum = Convert.ToInt32(request["curriculum"]);
			int publishType = Convert.ToInt32(request["publishType"]);
			string receiveObject = request["receiveObject"] ?? string.Empty;

			if (!Enum.IsDefined(typeof(DictHelper.WorkTypeEnum), workType))
			{
				throw new ApiArgumentException("参数workType不在指定的范围之内", 1);
			}
			if (sonWorkType < 0 || sonWorkType >= 100)
			{
				throw new ApiArgumentException("参数sonWorkType错误", 2);
			}
			if (!Enum.IsDefined(typeof(DictHelper.CurriculumEnum), curriculum))
			{
				throw new ApiArgumentException("参数curriculum不在指定的范围之内", 3);
			}
			if (!Enum.IsDefined(typeof(PublishTypeEnum), (byte)publishType))
			{
				throw new ApiArgumentException("参数publishType不在指定的范围之内", 4);
			}
			if (string.IsNullOrWhiteSpace(workName))
			{
				throw new ApiArgumentException("参数workName不能为空", 5);
			}

			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var receiveObjects = new List<KeyValuePair<long, string>>();
			try
			{
				receiveObjects = JSONHelper.Decode<List<KeyValuePair<long, string>>>(receiveObject);
			}
			catch (Exception e)
			{
				throw new ApiArgumentException("receiveObject序列化失败,Message:" + e.ToString(), 5);
			}
			var userInfo = new UserManager().GetUserInfo(userId);

			var workBase = new WorkBase()
			{
				WorkName = HttpUtility.UrlDecode(workName),
				WorkType = (DictHelper.WorkTypeEnum)workType,
				SonWorkType = sonWorkType,
				PublishUserID = userId,
				PublishUserName = userInfo.RealName,
				PublishDate = DateTime.Now,
				SendDate = DateTime.Now,
				EffectiveDate = DateTimeExtensions.FromEpoch(completeDate),
				IsSubmit = true,
				ViewStatus = ViewStatus.AllUsers,
				CompletedType = CompletedType.ReferLong,
				ReferLong = 30,
				CurriculumID = (DictHelper.CurriculumEnum)curriculum,
				PublishType = (PublishTypeEnum)publishType,
				WorkScore = 100,
				MarkType = MarkType.Auto,
				WorkDesc = workDesc,
				RedirectParm = string.Format("bid_{0}.sid_{1}.zuop_{2}", workPackageRecordId, 0, 0)
			};
			#endregion

			return new WorkPublisher().Publish(workBase, receiveObjects, null);
		}

		/// <summary>
		/// 获取做作业列表(我的作业)
		/// </summary>
		/// <param name="workStatus">作业状态: 待完成0, 已完成7, 已批阅:3</param>
		/// <param name="from">请求来源: 1:安卓 2:IOS </param>
		[HttpGet, BasicAuthentication]
		public dynamic my_doworks(int workStatus, int from, int versionId = 1, int page = 1, int pageSize = 10)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);

			var sonWorkTypes = new List<int>();
			if (from == 1)
			{
				sonWorkTypes = new List<int>() { 17, 18, 19, 23 };
			}
			else if (from == 2)
			{
				sonWorkTypes = new List<int>() { 23 };
			}
			int totalItem = 0;

			var list = new DoWorkBaseProvider().GetDoWorkListForApi(new List<int>() { userId }, null, sonWorkTypes, DateTime.MinValue, DateTime.Now, workStatus, -1, ref totalItem, page, pageSize);

			var workBaseList = new WorkBaseProvider().GetWorkBaseList(list.Select(t => t.WorkID).Distinct(), -1).ToDictionary(c => c.WorkID, c => c);

			var _workPublisher = new WorkPublisher();
			return new ApiPageList<object>()
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalItem,
				PageList = list.Where(t => workBaseList.Keys.Contains(t.WorkID)).Select(t =>
				{
					var _workBase = workBaseList[t.WorkID];
					return new
					{
						workId = t.WorkID,
						doworkId = t.DoWorkID,
						workName = t.WorkName,
						workType = t.WorkType,
						sonWorkType = t.SonWorkType,
						publishUserId = _workBase.PublishUserID,
						publishUserName = _workBase.PublishUserName ?? string.Empty,
						sendDate = _workBase.SendDate.Epoch(),
						publishType = _workBase.PublishType,
						reviceId = _workBase.ReviceUserID,
						reviceName = _workBase.ReviceUserName ?? string.Empty,
						totalNum = _workBase.TotalNum,
						completedNum = _workBase.CompletedNum,
						markNum = _workBase.MarkNum,
						workDesc = _workBase.WorkDesc ?? string.Empty,
						submitUserId = t.SubmitUserID,
						submitUserName = t.SubmitUserName ?? string.Empty,
						submitDate = _workBase.PublishDate.Epoch(),
						workStatus = t.WorkStatus,
						workLong = t.WorkLong,
						actualScore = t.ActualScore,
						workScore = t.WorkScore,
						effectiveDate = t.EffectiveDate.Epoch(),
						isTimeout = t.EffectiveDate < DateTime.Now,
						recordId = _workPublisher.redirectParmsArray(t.RedirectParm)
					};
				}).ToList()
			};
		}

		/// <summary>
		/// 老师获取自己布置的作业列表
		/// </summary>
		/// <param name="workStatus">作业状态:-1:全部 待完成:9  过期:10</param>
		/// <param name="sonWorkType">子作业类型 -1 全部</param>
		/// <param name="publishType">布置对象 -1:全部 0:个人布置 1班级布置 2孩子 4：班级小组</param>
		/// <param name="from">请求来源: 1:安卓 2:IOS </param>
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
			else if (from == 1)
			{
				sonWorkTypes = new List<int>() { 17, 18, 19, 23 };
			}
			else if (from == 2)
			{
				sonWorkTypes = new List<int>() { 23 };
			}

			int totalItem = 0;

			var workBaseList = new WorkBaseProvider().GetWorkBaseListForApi(new List<int>() { userId }, publishTypes, sonWorkTypes, reviceId, DateTime.MinValue, DateTime.Now, workStatus, -1, ref totalItem, page, pageSize);

			var _workPublisher = new WorkPublisher();

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
					sonWorkType = t.SonWorkType,
					publishUserId = t.PublishUserID,
					publishUserName = t.PublishUserName ?? string.Empty,
					sendDate = t.SendDate.Epoch(),
					effectiveDate = t.EffectiveDate.Epoch(),
					totalNum = t.TotalNum,
					completedNum = t.CompletedNum,
					markNum = t.MarkNum,
					reviceId = t.ReviceUserID,
					reviceName = t.ReviceUserName,
					recordId = _workPublisher.redirectParmsArray(t.RedirectParm),
					isTimeout = DateTime.Now > t.EffectiveDate
				})
			};
		}

		/// <summary>
		/// 获取该份作业的所有学生列表
		/// </summary>
		[HttpGet]
		public dynamic doworks(long workId)
		{
			var doWorkList = new DoWorkBaseProvider().GetDoWorkList(workId);

			return doWorkList.Select(t => new
			{
				workId = t.WorkID,
				doworkId = t.DoWorkID,
				workName = t.WorkName ?? string.Empty,
				submitUserId = t.SubmitUserID,
				submitUserName = t.SubmitUserName ?? string.Empty,
				submitDate = t.SubmitDate.Epoch(),
				workStatus = t.WorkStatus,
				workLong = t.WorkLong,
				actualScore = t.ActualScore,
				workScore = t.WorkScore
			});
		}
	}
}
