using CiWong.Framework.Helper;
using CiWong.Relation.WCFProxy;
using CiWong.Work.Contract;
using CiWong.Work.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.Work.Service
{
    /// <summary>
    /// 作业发布器
    /// </summary>
    public class WorkPublisher
    {
        private IWorkBase _workBase;
        private IClassGroupProvider _classGroupProvider;
		public WorkPublisher(IWorkBase _workBase, IClassGroupProvider _classGroupProvider)
		{
			this._workBase = _workBase;
			this._classGroupProvider = _classGroupProvider;
		}
    

        /// <summary>
        /// 发布作业
        /// </summary>
        /// <param name="workBase">发布作业信息</param>
        /// <param name="listPublishObject">做作业对象</param>
        /// <returns></returns>
        public List<long> Publish(WorkBase workBase, List<KeyValuePair<long, string>> listPublishObject, ReleaseRecord record)
        {
            var userList = new List<KeyValuePair<int, string>>();

            List<long> workIdList = new List<long>();

			var userSchool = ClassRelationProxy.GetRoomSchoolByUserList(new List<int>() { workBase.PublishUserID }).FirstOrDefault();

			if (userSchool != null)
			{
				workBase.SchoolId = userSchool.SchoolID;
				workBase.SchoolName = userSchool.SchoolName;
				workBase.AreaCode = userSchool.SchoolArea;
			}

			if (workBase.PublishType == PublishTypeEnum.User)
			{
				workBase.ReviceUserID = listPublishObject[0].Key;
				listPublishObject.ForEach(p => userList.Add(new KeyValuePair<int, string>((int)p.Key, p.Value)));
				if (_workBase.PublishWork(workBase, userList))
				{
					XiXinNotifyHelper.PublishWork(workBase, userList, record);
					workIdList.Add(workBase.WorkID);
				}
			}
			else if (workBase.PublishType == PublishTypeEnum.Class)
			{
				foreach (var item in listPublishObject)
				{
					var stuList = ClassRelationProxy.GetClassStudentMember(item.Key);
					if (!stuList.Any())
					{
						continue;
					}
					userList.Clear();
					stuList.ForEach(p => userList.Add(new KeyValuePair<int, string>(p.RoomUserID, p.RoomUserName)));
					workBase.ReviceUserName = item.Value;
					workBase.ReviceUserID = item.Key;
					if (_workBase.PublishWork(workBase, userList))
					{
						XiXinNotifyHelper.PublishWork(workBase, userList, record);
						workIdList.Add(workBase.WorkID);
					}
				}
			}
			else if (workBase.PublishType == PublishTypeEnum.Childen)
			{
				foreach (var item in listPublishObject)
				{
					userList.Clear();
					userList.Add(new KeyValuePair<int, string>((int)item.Key, item.Value));
					workBase.ReviceUserID = item.Key;
					workBase.ReviceUserName = item.Value;
					if (_workBase.PublishWork(workBase, userList))
					{
						XiXinNotifyHelper.PublishWork(workBase, userList, record);
						workIdList.Add(workBase.WorkID);
					}
				}
			}
			else if (workBase.PublishType == PublishTypeEnum.ClassGroup)
			{
				workBase.ReviceUserID = listPublishObject[0].Key;
				workBase.ReviceUserName = string.Join(",", listPublishObject.Take(3).Select(t => t.Value));
				workBase.ReviceUserName = listPublishObject.Count <= 3 ? workBase.ReviceUserName : workBase.ReviceUserName + "等";
				foreach (var item in listPublishObject)
				{
					var stuList = _classGroupProvider.getClassGroupMemberByGroupId(Convert.ToInt32(item.Key));
					if (!stuList.Any())
					{
						continue;
					}
					stuList.ForEach(p => userList.Add(new KeyValuePair<int, string>(p.StudentId, p.StudentName)));
				}
				if (_workBase.PublishWork(workBase, userList))
				{
					XiXinNotifyHelper.PublishWork(workBase, userList, record);
					workIdList.Add(workBase.WorkID);
				}
			}
            return workIdList;
        }

        /// <summary>
        /// 是否是来自WIKI
        /// </summary>
        /// <param name="workType"></param>
        /// <returns></returns>
        public bool IsWiki(DictHelper.WorkTypeEnum workType)
        {
            return workType == DictHelper.WorkTypeEnum.在线作业 || workType == DictHelper.WorkTypeEnum.电子题册 ||
                                    workType == DictHelper.WorkTypeEnum.仿真实验 || workType == DictHelper.WorkTypeEnum.玩转数学 ||
                                    workType == DictHelper.WorkTypeEnum.英语听写 || workType == DictHelper.WorkTypeEnum.语文听写;
        }

		/// <summary>
		/// 获取作业资源包ID
		/// </summary>
		internal string redirectParmsArray(string redirectParm, int index = 0)
		{
			if (string.IsNullOrEmpty(redirectParm) || redirectParm.IndexOf(".") == -1)
			{
				return string.Empty;
			}

			var _parmList = redirectParm.Replace("bid_", string.Empty)
										.Replace("sid_", string.Empty)
										.Replace("zuop_", string.Empty)
										.Split('.');

			return _parmList.Length > index ? _parmList[index] : string.Empty;
		}
    }
}
