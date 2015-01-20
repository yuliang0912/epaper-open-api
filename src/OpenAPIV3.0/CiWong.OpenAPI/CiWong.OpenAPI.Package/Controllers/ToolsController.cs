using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CiWong.Tools.Workshop.DataContracts;
using CiWong.Tools.Workshop.Services;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class ToolsController: ApiController
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
	        var moduleId = new Guid("a7527f97-14e6-44ef-bf73-3039033f128e");
	        var words = ResourceServices.Instance.GetByVersionIds(moduleId, new[] {versionId});

            //return new
            //{
            //    wId,
            //    words,
            //    wordFile,
            //    wordType,
            //    words,
            //    symbol,
            //    syllable,
            //    pretations,
            //    sentences,
            //    sentFile,
            //    wordPic
            //};

	    }
	}
}
