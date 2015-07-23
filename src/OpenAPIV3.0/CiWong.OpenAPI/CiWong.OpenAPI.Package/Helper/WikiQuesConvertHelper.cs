using CiWong.Framework.Helper;
using CiWong.Resource.Preview.DataContracts;
using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ToolsAndPackage.Helper
{
    /// <summary>
    /// 旧数据转换成新数据格式帮助类
    /// </summary>
    public static class WikiQuesConvertHelper
    {
        /// <summary>
        /// 试卷转换
        /// </summary>
        public static ExaminationContract ConvertExamination(CiWong.Examination.Mapping.Entities.Examination examination)
        {
            if (null == examination)
            {
                return null;
            }

            var examinationContract = new ExaminationContract()
            {
                Id = examination.ExaminationID,
                Title = examination.ExaminationTitle,
                RefScore = examination.RefScore,
                CurriculumID = examination.CurriculumID,
            };

            var parts = new List<QuestionModuleContract>();
            var index = 0;
            foreach (var group in examination.ExaminationVersions.GroupBy(t => t.ModulePosition))
            {
                parts.Add(new QuestionModuleContract()
                {
                    Sid = index++,
                    ModuleTypeURL = group.First().ModuleTypeURL,
                    ModuleTypeName = group.First().ModuleTypeName,
                    Children = ConvertQuestionList(group)
                });
            }

            examinationContract.Parts = parts;

            return examinationContract;
        }

        /// <summary>
        /// 题目批量转换
        /// </summary>
        public static List<QuestionContract> ConvertQuestionList(IEnumerable<CiWong.Examination.Mapping.Entities.ExaminationVersion> qlist)
        {
            var list = new List<QuestionContract>();
            var i = 0;
            foreach (var item in qlist)
            {
                if (null != item.Question && item.Question.ParentOfVersion == 0)
                {
                    var newQues = ConvertQuestion(item.Question, item.QuestionRefSorce);
                    newQues.Sid = i++;
                    if (item.Question.qType == QTtype.ClozeTest || item.Question.qType == QTtype.ReadingIdeal)
                    {
                        newQues.Children = qlist.Where(t => t.P_QuestionVersion == item.Question.Version)
                                                .Select(t => ConvertQuestion(t.Question, t.QuestionRefSorce)).ToList();
                        var j = 0;
                        newQues.Children.ToList().ForEach(t => t.Sid = j++);
                    }
                    list.Add(newQues);
                }
            }

            return list;
        }

        /// <summary>
        /// 题目批量转换
        /// </summary>
        public static List<QuestionContract> ConvertQuestionList(IEnumerable<Ques.Core.Models.Question> qlist)
        {
            var list = new List<QuestionContract>();

            foreach (var item in qlist)
            {
                if (item.ParentOfVersion == 0)
                {
                    var newQues = ConvertQuestion(item);
                    if (item.qType == QTtype.ClozeTest || item.qType == QTtype.ReadingIdeal)
                    {
                        newQues.Children = qlist.Where(t => t.ParentOfVersion == item.Version)
                                                .Select(t => ConvertQuestion(t)).ToList();
                    }
                    list.Add(newQues);
                }
            }

            return list;
        }

        /// <summary>
        /// 题目批量转换
        /// </summary>
        public static List<QuestionContract> ConvertQuestionList(IEnumerable<Ques.Core.Models.viewQuestion> qlist)
        {
            var list = new List<QuestionContract>();

            foreach (var item in qlist)
            {
                var newQues = ConvertQuestion(item.question);
                newQues.Children = item.questlt.Select(t => ConvertQuestion(t)).ToList();
                list.Add(newQues);
            }

            return list;
        }

        /// <summary>
        /// 题目转换
        /// </summary>
        public static QuestionContract ConvertQuestion(Ques.Core.Models.Question question, float questionRefScore = 0, bool isConvertRefInfo = true)
        {
            if (null == question)
            {
                return null;
            }

            var questionContract = new QuestionContract();

            questionContract.Id = question.QuestionID;
            questionContract.VersionId = question.Version;
            questionContract.Type = (int)question.qType;
            questionContract.Stem = null == question.Stem ? string.Empty : TemplateConverter.Instance.ConvertToHtml(question.Stem, TemplateConverter.Mathml, TemplateConverter.Mathlate, TemplateConverter.MathmlBlank, TemplateConverter.Math);
            questionContract.IsObjective = question.IsObjective;
            questionContract.QuestionRefSorce = questionRefScore;
            questionContract.ParentVersion = question.ParentOfVersion;
            questionContract.Opions = question.Option.Select(t => ConvertOption(t)).ToList();
            questionContract.Attachments = question.Attachments.Where(t => t.Type == AttachmentType.Problem).Select(t => ConvertAttachment(t)).ToList();

            if (isConvertRefInfo)
            {
                questionContract.RefInfo = ConvertRefInfo(question);
            }
            return questionContract;
        }

        /// <summary>
        /// 题目选项转换
        /// </summary>
        private static QuestionOption ConvertOption(Ques.Core.Models.OptionEntity optionEntity)
        {
            if (null == optionEntity)
            {
                return null;
            }

            return new QuestionOption()
            {
                Id = optionEntity.Id,
                Stem = TemplateConverter.Instance.ConvertToHtml(optionEntity.Name, TemplateConverter.Mathml, TemplateConverter.Mathlate, TemplateConverter.MathmlBlank, TemplateConverter.Math),
                Attachments = optionEntity.Attachments.Select(t => ConvertAttachment(t)).ToList()
            };
        }

        /// <summary>
        /// 题目参考信息(答案)转换
        /// </summary>
        private static QuestionRefInfo ConvertRefInfo(Ques.Core.Models.Question question)
        {
            if (question.Answer.Count > 1 && question.Answer[0].Answer == "")
            {
                question.Answer.RemoveAt(0);
            }
            return new QuestionRefInfo()
            {
                Answers = question.Answer.OrderBy(t => t.Sort).Select(t => TemplateConverter.Instance.ConvertToHtml(t.Answer, TemplateConverter.Mathml, TemplateConverter.Mathlate, TemplateConverter.MathmlBlank, TemplateConverter.Math)).ToArray(),
                SolvingIdea = question.WayOfObtain ?? string.Empty,
                Attachments = question.Attachments.Where(t => t.Type == AttachmentType.Ideas).Select(t => ConvertAttachment(t)).ToList()
            };
        }

        /// <summary>
        /// 附件转换
        /// </summary>
        /// <param name="attachment"></param>
        private static Attachment ConvertAttachment(Ques.Core.Models.AttachmentEntity attachment)
        {
            if (null == attachment)
            {
                return null;
            }

            return new Attachment()
            {
                Position = (int)attachment.Position,
                FileType = (int)attachment.fileType,
                FileUrl = attachment.URL
            };
        }
    }
}
