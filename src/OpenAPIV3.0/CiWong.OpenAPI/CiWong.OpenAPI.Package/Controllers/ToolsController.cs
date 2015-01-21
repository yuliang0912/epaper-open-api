﻿using System;
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
        /// 获取课后单词表资源
        /// 说明:选择单词.只有资源类型ID=a7527f97-14e6-44ef-bf73-3039033f128e时才有单词选择
        /// </summary>
        /// <param name="versionId">资源版本ID(此处传父资源parentVersionId),必选</param>
        /// <returns></returns>
        [HttpGet]
        public object followread_word_details(long versionId)
        {
            var result = ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.SyncFollowRead, versionId);
            if (result.IsSucceed)
            {
                var data = (SyncReadContract) result.Data.FirstOrDefault();
                if (data == null)
                {
                    throw new ApiArgumentException("参数versionId错误，未找到指定资源");
                }
                var wordVersionList =
                    data.Parts.Where(t => t.ModuleId == ResourceModuleOptions.Word)
                        .Select(t => t.VersionId)
                        .Where(t => t != null)
                        .OfType<long>();

                var wordList= ResourceServices.Instance.GetByVersionIds(ResourceModuleOptions.Word, wordVersionList.ToArray());
                if (wordList.IsSucceed)
                {
                    var words = wordList.Data.Where(t => t != null).OfType<WordContract>();
                    return words.Select(x => new
                    {
                        wId =x.Id,
                        words = x.Name,
                        wordFile = x.AudioUrl,
                        wordType = x.IsExpand,
                        symbol = x.Symbol,
                        syllable = x.Syllable,
                        pretations = x.Pretations,
                        sentences = x.Sentences,
                        sentFile = x.AudioUrl,
                        wordPic = x.PictureUrl
                    });
                }
                else
                {
                    throw new ApiException(RetEum.ApplicationError, 2, "内部代码异常");
                }
            }
            else
            {
                throw new ApiException(RetEum.ApplicationError, 1, "内部代码异常");
            }
        }
    }
}
