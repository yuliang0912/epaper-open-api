using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CiWong.OpenAPI.ToolsAndPackage.Controllers
{
	public class ToolsController: ApiController
	{
		[HttpGet]
		public int test()
		{
			return 2;
		}
	}
}
