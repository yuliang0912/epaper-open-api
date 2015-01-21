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
	            wId = x.Id,
	            words = x.Name ?? "",
	            wordFile = x.AudioUrl ?? "",
	            wordType = x.IsExpand == true ? 1 : 0,
	            symbol = x.Symbol ?? "",
	            syllable = x.Syllable ?? "",
	            pretations = x.Pretations ?? "",
	            sentences = x.Sentences.Any() ? x.Sentences.First().Text : "",
	            sentFile = x.Sentences.Any() ? x.Sentences.First().AudioUrl : "",
	            wordPic = x.PictureUrl ?? ""
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
                throw new ApiException(RetEum.ApplicationError, 2, "内部代码异常");
            }
            var data = (SyncFollowReadContract)result.Data.FirstOrDefault();
            if (data == null)
            {
                throw new ApiArgumentException("参数versionId错误，未找到指定资源");
            }
            var versionList =
                data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.SyncFollowReadText)
                    .SelectMany(t => t.List).Select(t => t.VersionId)
                    .Where(t => t != null)
                    .OfType<long>();

            var phraseList = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.Phrase,
                versionList.ToArray());

            if (!phraseList.IsSucceed)
            {
                throw new ApiException(RetEum.ApplicationError, 1, "内部代码异常");
            }
            var phrases = phraseList.Data.Where(t => t != null).OfType<PhraseContract>();
            return phrases.Select(x => new
            {
                content = x.Content ?? "",
                audioUrl = x.AudioUrl ?? "",
                versionId = x.VersionId,
                resourceModuleId = x.ModuleId,
                name = x.Name ?? ""
            });
        }
	}
}
