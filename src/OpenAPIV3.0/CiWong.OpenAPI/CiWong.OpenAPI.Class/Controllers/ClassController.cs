using CiWong.Relation.WCFProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace CiWong.OpenAPI.Class.Controllers
{
	public class ClassController : ApiController
	{
		/// <summary>
		/// 获取第一个学校和班级
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic first_school_class_info(int userId)
		{
			var classList = ClassRelationProxy.GetRoomClass(userId);

			if (!classList.Any())
			{
				return null;
			}

			var classInfo = classList.First();
			var schoolInfo = ClassRelationProxy.GetRoomSchool(classInfo.SchoolID);

			if (null == schoolInfo)
			{
				return null;
			}

			return new
			{
				classId = classInfo.ClassID,
				className = classInfo.ClassName,
				periodId = classInfo.PeriodID,
				gradeId = classInfo.GradeID,
				schoolId = schoolInfo.SchoolID,
				schoolName = schoolInfo.SchoolName
			};
		}

		/// <summary>
		/// 获取班级列表
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic class_list(int userId, int top = 0)
		{
			var classList = ClassRelationProxy.GetRoomClass(userId);

			if (top > 0)
			{
				classList = classList.Take(top).ToList();
			}

			return classList.Select(x => new
			{
				classId = x.ClassID,
				className = x.ClassName,
				studentNum = ClassRelationProxy.GetClassMemberCountByClassId(x.ClassID, 1)
			});
		}

		/// <summary>
		/// 获取班级列表
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic teach_class_list(int userId, int top = 10)
		{
			var classList = ClassRelationProxy.GetTeacherClassByUserId(userId, top);

			return classList.Select(x => new
			{
				classId = x.ID,
				className = x.GroupClassName,
				studentNum = ClassRelationProxy.GetClassMemberCountByClassId(x.ID, 1)
			});
		}

		/// <summary>
		/// 获取班级成员
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		[HttpGet]
		public dynamic class_student_members(long classId)
		{
			return ClassRelationProxy.GetClassStudentMember(classId).Select(t => new
			{
				userId = t.RoomUserID,
				userName = t.RoomUserName
			});
		}

		[HttpGet, HttpPost]
		public List<long> list_school_id(int userId)
		{
			return ClassRelationProxy.GetRoomSchoolByUserList(new List<int>() { userId }).Select(t => t.SchoolID).ToList();
		}
	}
}

			