using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CiWong.OpenAPI.Core;
using CiWong.Tools.Workshop.DataContracts;
using CiWong.Tools.Workshop.Services;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
    public class ToolsController : ApiController
    {
        /// <summary>
        /// 根据同步跟读ID获取课后单词表资源
        /// </summary>
        /// <param name="versionId">同步跟读资源版本ID,必选</param>
        /// <returns></returns>
        [HttpGet]
        public object followread_word_details(long versionId)
        {
            var result = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.SyncFollowRead, versionId);
            if (!result.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 2, "内部代码异常");
            }
            var data = (SyncFollowReadContract) result.Data.FirstOrDefault();
            if (data == null)
            {
                throw new ApiArgumentException("参数versionId错误，未找到指定资源");
            }
            var wordVersionList =
                data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word)
                    .SelectMany(t => t.List).Select(t => t.VersionId)
                    .Where(t => t != null)
                    .OfType<long>();

            var wordList = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.Word,
                wordVersionList.ToArray());

            if (!wordList.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 1, "内部代码异常");
            }
            var words = wordList.Data.Where(t => t != null).OfType<WordContract>();
            return words.Select(x => new
            {
                wId = x.Id ?? 0,
                words = x.Name ?? string.Empty,
                wordFile = x.AudioUrl ?? string.Empty,
                wordType = x.IsExpand.HasValue ? x.IsExpand.Value ? 1 : 0 : 0,
                symbol = x.Symbol ?? string.Empty,
                syllable = x.Syllable ?? string.Empty,
                pretations = x.Pretations ?? string.Empty,
                sentences = x.Sentences.Any() ? x.Sentences.First().Text ?? string.Empty : string.Empty,
                sentFile = x.Sentences.Any() ? x.Sentences.First().AudioUrl ?? string.Empty : string.Empty,
                wordPic = x.PictureUrl ?? string.Empty
            });
        }

        /// <summary>
        /// 获取选择句子
        /// </summary>
        /// <param name="versionId">资源版本ID(课文ID),必选</param>
        /// <returns></returns>
        [HttpGet]
        public object followread_text_sentences(long versionId)
        {
            var result = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.SyncFollowRead, versionId);
            if (!result.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 1, "内部代码异常");
            }
            var data = (SyncFollowReadContract) result.Data.FirstOrDefault();
            if (data == null)
            {
                throw new ApiArgumentException("参数versionId错误，未找到指定资源");
            }
            var versionList =
                data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.SyncFollowReadText)
                    .SelectMany(t => t.List).Select(t => t.VersionId)
                    .Where(t => t != null)
                    .OfType<long>();

            var textList = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.SyncFollowReadText,
                versionList.ToArray());

            if (!textList.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 2, "内部代码异常");
            }

            var texts = textList.Data.Where(t => t != null).OfType<SyncFollowReadTextContract>();
            return texts.SelectMany(t => t.Sections).SelectMany(t => t.Sentences).Select(x => new
            {
                content = x.Content ?? string.Empty,
                audioUrl = x.AudioUrl ?? string.Empty,
                versionId = x.VersionId ?? 0,
                resourceModuleId = x.ModuleId ?? Guid.Empty,
                name = x.Name ?? string.Empty,
            });
        }

        private object QuestionFunc(QuestionContract m)
        {
            return new
            {
                type = m.Type,
                versionId = m.VersionId ?? 0,
                moduleId = m.ModuleId ?? Guid.Empty,
                trunk = new
                {
                    body = m.Trunk != null ? m.Trunk.Body ?? string.Empty : string.Empty,
                    attachments = m.Trunk != null
                        ? m.Trunk.Attachments.Select(p => new
                        {
                            id = p.Id,
                            name = p.Name ?? string.Empty,
                            url = p.Url ?? string.Empty,
                            fileType = p.FileType,
                            position = (int) p.Position
                        })
                        : Enumerable.Empty<object>()
                },
                options = m.Options.Select(o => new
                {
                    id = o.Id,
                    isAnswer = o.IsAnswer ? 1 : 0,
                    value = o.Value.Select(u => new
                    {
                        body = u.Body ?? string.Empty,
                        attachments = u.Attachments != null
                            ? u.Attachments.Select(p => new
                            {
                                id = p.Id,
                                name = p.Name ?? string.Empty,
                                url = p.Url ?? string.Empty,
                                fileType = p.FileType,
                                position = (int) p.Position
                            })
                            : Enumerable.Empty<object>()

                    })
                }),
                children =
                    m.Children != null ? m.Children.Select(t => QuestionFunc(t)) : Enumerable.Empty<QuestionContract>()
            };
        }

        /// <summary>
        /// 获取模考资源
        /// </summary>
        /// <param name="versionId">资源版本ID,必选</param>
        /// <returns></returns>
        [HttpGet]
        public object listenspeak_exam(long versionId)
        {
            var result = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.ListeningAndSpeaking, versionId);
            if (!result.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 1, "内部代码异常");
            }

            var x = result.Data.Where(t => t != null).OfType<ListeningAndSpeakingContract>().FirstOrDefault();
            if (x == null)
            {
                throw new ApiException(RetEum.ApplicationError, 1, "未找到资源");
            }
            return new
            {
                totalScore = x.TotalScore,
                limitTime = x.LimitTime,
                versionId = x.VersionId ?? 0,
                moduleId = x.ModuleId ?? Guid.Empty,
                items = x.Items.Select(t => new
                {
                    templateSettings = new
                    {
                        content = t.TemplateSettings.Content ?? string.Empty,
                        audioUrl = t.TemplateSettings.AudioUrl ?? string.Empty,
                        questionNumber = t.TemplateSettings.QuestionNumber,
                        listeningAndSpeakingRule = new
                        {
                            audioViews =
                                t.TemplateSettings.ListeningAndSpeakingRule != null
                                    ? t.TemplateSettings.ListeningAndSpeakingRule.AudioViews
                                    : 0,
                            lookTime =
                                t.TemplateSettings.ListeningAndSpeakingRule != null
                                    ? t.TemplateSettings.ListeningAndSpeakingRule.LookTime
                                    : 0,
                            answerTime =
                                t.TemplateSettings.ListeningAndSpeakingRule != null
                                    ? t.TemplateSettings.ListeningAndSpeakingRule.AnswerTime
                                    : 0
                        }
                    },
                    scores = t.Scores.Select(m => new
                    {
                        questionVersionId = m.QuestionVersionId,
                        score = m.Score
                    }),
                    questions = t.Questions.Select(m => QuestionFunc(m))
                })
            };
        }
    }
}
