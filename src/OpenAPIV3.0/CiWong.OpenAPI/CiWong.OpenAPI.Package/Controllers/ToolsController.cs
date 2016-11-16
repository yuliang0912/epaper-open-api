using System;
using System.Linq;
using System.Web.Http;
using CiWong.OpenAPI.Core;
using CiWong.OpenAPI.Core.Extensions;
using CiWong.Tools.Workshop.DataContracts;
using CiWong.Tools.Workshop.Services;
using CiWong.OpenAPI.ToolsAndPackage.Helper;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class ToolsController : ApiController
	{
		/// <summary>
		/// 根据同步跟读ID获取课后单词列表
		/// </summary>
		/// <param name="versionId">资源版本ID(此处传父资源parentVersionId),必选</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic followread_words(long versionId)
		{
			var result = ResourceServices.Instance.GetByVersionId<SyncFollowReadContract>(versionId);
			if (!result.IsSucceed)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5003, "资源接口内部异常错误");
			}
			if (result.Data == null)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5002, "参数versionId错误，未找到指定资源");
			}
			return result.Data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word).SelectMany(t => t.List).Select(t => new
			{
				id = t.Id ?? 0,
				versionId = t.VersionId ?? 0,
				name = t.Name ?? string.Empty,
				resourceModuleId = t.ModuleId ?? Guid.Empty
			});
		}

		/// <summary>
		/// 根据同步跟读ID获取课后单词表资源
		/// </summary>
		/// <param name="versionId">同步跟读资源版本ID,必选</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic followread_word_details(long versionId)
		{
			var result = ResourceServices.Instance.GetByVersionId<SyncFollowReadContract>(versionId);
			if (!result.IsSucceed)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5003, "资源接口内部异常错误");
			}
			if (result.Data == null)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5002, "参数versionId错误，未找到指定资源");
			}

			var wordVersionList = result.Data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word)
				.SelectMany(t => t.List).Where(t => t.VersionId.HasValue).Select(t => t.VersionId.Value).ToArray();

			var wordList = ResourceServices.Instance.GetByVersionIds<WordContract>(wordVersionList);

			if (!wordList.IsSucceed)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5003, "资源接口内部异常错误");
			}

			return wordList.Data.Select(x => new
			{
				wId = x.VersionId ?? 0,
				words = x.Name ?? string.Empty,
				wordFile = ToolsHelper.ConverAudioUrl(x.AudioUrl),
				wordType = x.IsExpand.HasValue && x.IsExpand.Value,
				symbol = x.Symbol ?? string.Empty,
				syllable = x.Syllable ?? string.Empty,
				pretations = x.Pretations ?? string.Empty,
				sentences = x.Sentences.Any() ? x.Sentences.First().Text ?? string.Empty : string.Empty,
				sentFile = x.Sentences.Any() ? ToolsHelper.ConverAudioUrl(x.Sentences.First().AudioUrl) : string.Empty,
				wordPic = x.PictureUrl ?? string.Empty
			});
		}

		/// <summary>
		/// 获取选择句子
		/// </summary>
		/// <param name="versionId">资源版本ID(课文ID),必选</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic followread_text_sentences(long versionId)
		{
			var result = ResourceServices.Instance.GetByVersionId<SyncFollowReadTextContract>(versionId);
			if (!result.IsSucceed)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5003, "资源接口内部异常错误");
			}

			if (result.Data == null)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5002, "参数versionId错误，未找到指定资源");
			}

			return result.Data.Sections.SelectMany(t => t.Sentences).Select(x => new
			{
				content = x.Content ?? string.Empty,
				audioUrl = ToolsHelper.ConverAudioUrl(x.AudioUrl),
				versionId = x.VersionId ?? 0,
				resourceModuleId = x.ModuleId ?? Guid.Empty,
				name = x.Name ?? string.Empty,
			});
		}

		/// <summary>
		/// 获取模考资源
		/// </summary>
		/// <param name="versionId">资源版本ID,必选</param>
		/// <returns></returns>
		[HttpGet]
		public dynamic listenspeak_exam(long versionId)
		{
			var result = ResourceServices.Instance.GetByVersionId<ListeningAndSpeakingContract>(versionId);
			if (!result.IsSucceed)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5003, "资源接口内部异常错误");
			}

			if (result.Data == null)
			{
				return new ApiArgumentException(ErrorCodeEum.Resource_5002, "参数versionId错误，未找到指定资源");
			}

			return new
			{
				totalScore = result.Data.TotalScore,
				limitTime = result.Data.LimitTime,
				versionId = result.Data.VersionId ?? 0,
				moduleId = result.Data.ModuleId ?? Guid.Empty,
				items = result.Data.Items.Select(t => new
				{
					templateSettings = new
					{
						content = t.TemplateSettings.Content ?? string.Empty,
						audioUrl = ToolsHelper.ConverAudioUrl(t.TemplateSettings.AudioUrl),
						questionNumber = t.TemplateSettings.QuestionNumber,
						listeningAndSpeakingRule = new
						{
							audioViews = t.TemplateSettings.ListeningAndSpeakingRule.AudioViews,
							lookTime = t.TemplateSettings.ListeningAndSpeakingRule.LookTime,
							answerTime = t.TemplateSettings.ListeningAndSpeakingRule.AnswerTime,
							readyTime = t.TemplateSettings.ListeningAndSpeakingRule.ReadyTime,
							//大题音频播放次数.
							rootAudioViews = t.TemplateSettings.ListeningAndSpeakingRule.RootAudioViews,
							//大题看题时间
							rootLookTime = t.TemplateSettings.ListeningAndSpeakingRule.RootLookTime,
							//大题准备时间
							rootReadyTime = t.TemplateSettings.ListeningAndSpeakingRule.RootReadyTime
						}
					},
					scores = t.Scores.Select(m => new
					{
						questionVersionId = m.QuestionVersionId,
						score = m.Score
					}),
					questions = t.Questions.Select(ToolsHelper.QuestionFunc)
				})	
			};
		}
	}
}
