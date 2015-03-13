using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.Package.Helper
{
	public static class ToolsHelper
	{
		public static Dictionary<int, string> moduleNameDict = new Dictionary<int, string>();

		static ToolsHelper()
		{
			moduleNameDict.Add(5, "单元测试");
			moduleNameDict.Add(7, "时文");
			moduleNameDict.Add(9, "同步讲练");
			moduleNameDict.Add(10, "同步跟读");
			moduleNameDict.Add(15, "听说模考");
			moduleNameDict.Add(18, "技能训练");
		}
	}
}
