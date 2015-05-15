using CiWong.Agent.ApiCore;
using CiWong.OpenAPI.Core;
using CiWong.Users;
using System;
using System.Linq;
using System.Threading;
using System.Web.Http;

namespace CiWong.OpenAPI.Agent.Controllers
{
	/// <summary>
	/// 代理商后台
	/// </summary>
	public class AgentController : ApiController
	{
		/// <summary>
		/// 为班级申请试用服务
		/// </summary>
		/// <param name="classIds"></param>
		/// <param name="serviceType">服务类型,阳光英语:25</param>
		/// <returns></returns>
		//[BasicAuthentication, HttpPost]
		//public dynamic apply_services(string classIds, int serviceType)
		//{
		//	var classList = classIds.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Select(t => Convert.ToInt64(t)).ToList();

		//	int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
		//	var userInfo = new UserManager().GetUserInfo(userId);

		//	return AppServiceProxy.ApplyUseService(userId, userInfo.RealName, classList, serviceType).Select(t => new
		//	{
		//		classId = t.Key,
		//		isSuccess = t.Value.ret == 0,
		//		ret = t.Value.ret,
		//		msg = t.Value.msg
		//	});
		//}

		/// <summary>
		/// 为班级申请试用服务
		/// </summary>
		/// <param name="classIds"></param>
		/// <param name="serviceType">服务类型,阳光英语:25</param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic apply_service(long classId, int serviceType)
		{
			int userId = Convert.ToInt32(Thread.CurrentPrincipal.Identity.Name);
			var userInfo = new UserManager().GetUserInfo(userId);

			var result = AppServiceProxy.ApplyUseService(userId, userInfo.RealName, classId, serviceType);

			return new ApiResult()
			{
				Ret = (RetEum)result.ret,
				ErrorCode = result.errcode,
				Message = result.msg
			};
		}

		/// <summary>
		/// 获取班级申请情况记录
		/// </summary>
		/// <param name="classIds"></param>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic class_apply_record(string classIds, int serviceType)
		{
			var classList = classIds.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Select(t => Convert.ToInt64(t)).ToList();

			return AppServiceProxy.GetOpenServiceList(classList, serviceType).Select(t => new
			{
				classId = t.ClassId,
				className = t.ClassName,
				serviceType = t.ServiceType,
				useDayNum = t.UseDayNum,
				expireTime = t.ExpireTime,
				isDelay = t.IsDelay,
				isTimeOut = t.ExpireTime < DateTime.Now
			});
		}

		/// <summary>
		/// 获取用户是否拥有资源包的使用服务权限
		/// </summary>
		/// <param name="packageId"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		[BasicAuthentication, HttpGet]
		public dynamic open_service_list(long packageId, int userId)
		{
			return AppServiceProxy.GetOpenServiceList(userId, packageId).Select(t => new
			{
				userId = t.UseId,
				userName = t.UseName,
				serviceType = t.ServiceType,
				serviceTypeName = t.ServiceTypeName,
				price = t.Price,
				expireTime = t.ExpireTime,
				isTimeOut = Convert.ToDateTime(t.ExpireTime) < DateTime.Now
			});
		}
	}
}
