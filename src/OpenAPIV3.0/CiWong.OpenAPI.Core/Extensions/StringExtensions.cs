using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CiWong.OpenAPI.Core.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// 去除HTML标签
		/// </summary>
		/// <param name="htmlString"></param>
		/// <returns></returns>
		public static string RemoveHtml(this string htmlString)
		{
			if (string.IsNullOrWhiteSpace(htmlString))
			{
				return htmlString;
			}

			htmlString = Regex.Replace(htmlString, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"-->", "", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"<!--.*", "", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(nbsp|#160);", " ", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
			htmlString = Regex.Replace(htmlString, @"&#(\d+);", "", RegexOptions.IgnoreCase);
			htmlString.Replace("<", "");
			htmlString.Replace(">", "");
			htmlString.Replace("\r\n", "");

			return htmlString;
		}

		public static List<string> MatchsFileUrl(string content)
		{
			var list = new List<string>();

			//Regex re = new Regex(@"(http://)([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})(:[0-9]{1,4})*(/[a-zA-Z0-9\&%_\./-~-]*)?", RegexOptions.Compiled);

			Regex re = new Regex(@"(http://)([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})(:[0-9]{1,4})*(/[a-zA-Z0-9\&%_\./-]*)?", RegexOptions.Compiled);

			MatchCollection mc = re.Matches(content);

			foreach (Match item in mc)
			{
				if (item.Value.LastIndexOf("/") > 7 && item.Value.IndexOf("ciwong.") > -1)
				{
					list.Add(item.Value);
				}
			}

			return list;
		}

		/// <summary>
		/// 分割字符串
		/// </summary>
		/// <param name="str"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static string CutString(this string str, int length)
		{
			if (string.IsNullOrEmpty(str) || str.Length < length)
			{
				return str;
			}
			return str.Substring(0, length);
		}
	}
}
