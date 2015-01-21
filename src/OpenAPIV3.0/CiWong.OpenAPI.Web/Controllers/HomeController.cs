using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Newtonsoft.Json;
using CiWong.OpenAPI.Core;
using System.Web.Http;
using System.Threading;

namespace CiWong.OpenAPI.Web.Controllers
{
	[BasicAuthentication]
	public class HomeController : ApiController
	{
		[HttpGet]
		public TestClass test1()
		{
			string content = "{\"userId\":155014,\"classId\":\"456451816546546456\",\"regTime\":1256227200}";

			var tt = JSONHelper.Decode<TestClass>(content);

			var tuser = Thread.CurrentPrincipal.Identity.Name;

			var ss = new TestClass() { ClassId = 456451816546546456, UserId = 155014, RegTime = new DateTime(2009, 10, 23), Sex = sexEnum.b };

			var scon = JSONHelper.Encode<TestClass>(ss);

			return new TestClass() { ClassId = 456451816546546456, UserId = 155014, RegTime = new DateTime(2009, 10, 23), Sex = sexEnum.b };
		}
	}

	public class TestClass
	{
		public int UserId { get; set; }

		public long ClassId { get; set; }
			
		public DateTime RegTime { get; set; }

		public sexEnum Sex { get; set; }
	}

	public enum sexEnum
	{
		b = 1,
		g = 2
	}
}