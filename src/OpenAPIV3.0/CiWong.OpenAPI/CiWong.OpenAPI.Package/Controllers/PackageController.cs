using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CiWong.OpenAPI.Core;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class PackageController : ApiController
	{
		[HttpGet]
		public int test(int age = 1)
		{
			return age = 1;
		}
	}
}
