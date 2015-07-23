using CiWong.Framework.Helper;
using CiWong.Resource.Preview.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CiWong.OpenAPI.ToolsAndPackage.Helper
{
	/// <summary>
	/// 听说模考试卷转换成维基试卷
	/// </summary>
	public static class ListenConvertHepler
	{
		public static ExaminationContract ConvertSpeak(CiWong.Tools.Workshop.DataContracts.ListeningAndSpeakingContract listeningAndSpeaking)
		{
			if (null == listeningAndSpeaking)
			{
				return null;
			}

			var examinationContract = new ExaminationContract()
			{
				Id = listeningAndSpeaking.Id.HasValue ? listeningAndSpeaking.Id.Value : 0,
				Title = listeningAndSpeaking.Name,
				RefScore = Convert.ToSingle(listeningAndSpeaking.TotalScore),
				CurriculumID = 3
			};

			var parts = new List<QuestionModuleContract>();

			listeningAndSpeaking.Items.ToList().ForEach(t => parts.Add(new QuestionModuleContract()
			{
				ModuleTypeName = t.TemplateSettings.Content ?? string.Empty,
				Children = ConvertQuestion(t)
			}));

			examinationContract.Parts = parts;

			return examinationContract;
		}

		public static List<QuestionContract> ConvertQuestion(CiWong.Tools.Workshop.DataContracts.ListeningAndSpeakingItem listeningAndSpeakingItem)
		{
			var list = new List<QuestionContract>();

			if (null == listeningAndSpeakingItem.Questions || !listeningAndSpeakingItem.Questions.Any())
			{
				return list;
			}

			var questionScores = listeningAndSpeakingItem.Scores.ToDictionary(t => t.QuestionVersionId, t => t.Score);

			foreach (var item in listeningAndSpeakingItem.Questions)
			{
				var question = ConvertQuestion(item, 0, questionScores.ContainsKey(item.VersionId.Value) ? questionScores[item.VersionId.Value] : 0m);

				if (null != item.Children && item.Children.Any())
				{
					question.Children = item.Children.Select(t => ConvertQuestion(t, item.VersionId.Value, questionScores.ContainsKey(t.VersionId.Value) ? questionScores[t.VersionId.Value] : 0m));
				}

				list.Add(question);
			}

			return list;
		}


		/// <summary>
		/// 题目转换
		/// </summary>
		public static QuestionContract ConvertQuestion(CiWong.Tools.Workshop.DataContracts.QuestionContract question, long parentVersion = 0, decimal questionRefScore = 0, bool isConvertRefInfo = true)
		{
			if (null == question)
			{
				return null;
			}

			var questionContract = new QuestionContract();

			questionContract.Id = question.Id;
			questionContract.VersionId = question.VersionId;
			questionContract.Type = (int)question.Type;
			questionContract.Stem = TemplateConverter.Instance.ConvertToHtml(question.Trunk.Body, TemplateConverter.Mathml, TemplateConverter.Mathlate, TemplateConverter.MathmlBlank, TemplateConverter.Math);
			questionContract.IsObjective = question.IsObjective;
			questionContract.QuestionRefSorce = Convert.ToSingle(questionRefScore);
			questionContract.ParentVersion = parentVersion;
			questionContract.Opions = question.Options.Select(t => ConvertOption(t)).ToList();
			if (null != question.Trunk.Attachments)
			{
				questionContract.Attachments = question.Trunk.Attachments.Select(t => ConvertAttachment(t)).ToList();
			}
			if (isConvertRefInfo)
			{
				questionContract.RefInfo = ConvertRefInfo(question);
			}
			return questionContract;
		}

		/// <summary>
		/// 题目选项转换
		/// </summary>
		private static QuestionOption ConvertOption(CiWong.Tools.Workshop.DataContracts.QuestionOption optionEntity)
		{
			if (null == optionEntity)
			{
				return null;
			}
			if (!optionEntity.Value.Any())
			{
				return new QuestionOption();
			}

			var question = new QuestionOption()
			{
				Id = optionEntity.Id,
				Stem = TemplateConverter.Instance.ConvertToHtml(optionEntity.Value.First().Body, TemplateConverter.Mathml, TemplateConverter.Mathlate, TemplateConverter.MathmlBlank, TemplateConverter.Math)
			};

			if (null != optionEntity.Value.First().Attachments)
			{
				question.Attachments = optionEntity.Value.First().Attachments.Select(t => ConvertAttachment(t)).ToList();
			}
			return question;
		}

		/// <summary>
		/// 附件转换
		/// </summary>
		/// <param name="attachment"></param>
		private static Attachment ConvertAttachment(CiWong.Tools.Workshop.DataContracts.QuestionAttachment attachment)
		{
			if (null == attachment)
			{
				return null;
			}

			return new Attachment()
			{
				Position = (int)attachment.Position,
				FileType = (int)attachment.FileType,
				FileUrl = attachment.Url
			};
		}

		/// <summary>
		/// 题目参考信息(答案)转换
		/// </summary>
		private static QuestionRefInfo ConvertRefInfo(CiWong.Tools.Workshop.DataContracts.QuestionContract question)
		{
			if (null == question)
			{
				return null;
			}

			if (null == question.Options || !question.Options.Any() || !question.Options.First().Value.Any())
			{
				return new QuestionRefInfo();
			}

			if (question.Type == 1 || question.Type == 2)
			{
				return new QuestionRefInfo()
				{
					Answers = question.Options.Where(t => t.IsAnswer).Select(t => t.Id.ToString()).ToArray()
				};
			}
			else
			{
				return new QuestionRefInfo()
				{
					Answers = question.Options.Where(t => t.IsAnswer).Select(t => t.Value.First().Body).ToArray()
				};
			}
		}
	}
}
