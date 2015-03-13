﻿using CiWong.Relation.WCFProxy;
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
		public dynamic class_list(int userId)
		{
			var classList = ClassRelationProxy.GetRoomClass(userId);

			return classList.Select(x => new
			{
				classId = x.ClassID,
				className = x.ClassName,
				studentNum = ClassRelationProxy.GetClassMemberCountByClassId(x.ClassID, 1)
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
	}
}

			