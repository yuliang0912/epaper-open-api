using CApi.Client;
using CiWong.Framework.Helper;
using CiWong.Work.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.Work.Service
{
    public static class XiXinNotifyHelper
    {
        public static void PublishWork(WorkBase workBase, List<KeyValuePair<int, string>> userList, ReleaseRecord record)
        {
            string viewUrl = string.Empty;//预览地址
            string bookId = redirectParmsArray(workBase.RedirectParm, 0);
            string sectionId = redirectParmsArray(workBase.RedirectParm, 1);
            switch (workBase.WorkType)
            {
                case DictHelper.WorkTypeEnum.在线作业:
                    viewUrl = string.Format("http://exam.ciwong.com/shijuan-{0}", workBase.ExaminationID);
                    break;
                case DictHelper.WorkTypeEnum.仿真实验:
                    viewUrl = string.Format("http://cer.ciwong.com/book/sectiontest?sectionid={0}&bookid={1}", sectionId, bookId);
                    break;
                case DictHelper.WorkTypeEnum.电子题册:
                    viewUrl = string.Format("http://ebook.ciwong.com/ebook/priviewdetails?sectionid={0}&bookid={1}", sectionId, bookId);
                    break;
                case DictHelper.WorkTypeEnum.智力游戏:
                    viewUrl = string.Format("http://game.ciwong.com/game/info/{0}", bookId);
                    break;
                case DictHelper.WorkTypeEnum.在线听力:
                    viewUrl = string.Format("http://listen.ciwong.com/story/detail/{0}?documentid={1}", bookId, sectionId);
                    break;
                case DictHelper.WorkTypeEnum.语文听写板:
                    viewUrl = string.Format("http://notebook.ciwong.com/listenboard/do/index?channel=90&doworkid=0&test=0&id={0}", bookId);
                    break;
                case DictHelper.WorkTypeEnum.英语听写板:
                    viewUrl = string.Format("http://notebook.ciwong.com/listenboard/do/index?channel=91&doworkid=0&test=0&id={0}", bookId);
                    break;
                case DictHelper.WorkTypeEnum.信息作品制作:
                    viewUrl = record == null ? string.Empty : string.Format("http://zuopin.ciwong.com/zuopin/preview/{0}", record.RecordID);
                    break;
                case DictHelper.WorkTypeEnum.语文作文:
                    viewUrl = record == null ? string.Empty : string.Format("http://zuowen.ciwong.com/yuwen/preview/{0}", record.RecordID);
                    break;
                case DictHelper.WorkTypeEnum.英语作文:
                    viewUrl = record == null ? string.Empty : string.Format("http://zuowen.ciwong.com/english/preview/{0}", record.RecordID);
                    break;
                default:
                    break;
            }
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                    new NotifyClient().PublishWorkRequst(workBase.PublishUserID, workBase.PublishUserName, workBase.WorkName, viewUrl, "http://w.ciwong.com/teacher/Redirect?workId=" + workBase.WorkID, workBase.SendDate, workBase.EffectiveDate, userList.Select(t => t.Key).ToArray()));
        }

        private static string redirectParmsArray(string redirectParm, int index)
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
