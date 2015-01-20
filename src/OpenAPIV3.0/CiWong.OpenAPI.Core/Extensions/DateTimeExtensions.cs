using System;

namespace CiWong.OpenAPI.Core.Extensions
{
	public static class DateTimeExtensions
	{
		private static readonly DateTime spanDate = new DateTime(1970, 1, 1);

		/// <summary>
		/// DateTime转换时间戳(秒)
		/// </summary>
		public static int Epoch(this DateTime time)
		{
			return (int)(time.ToUniversalTime() - spanDate).TotalSeconds;
		}

		/// <summary>
		/// 时间戳(秒)转DateTime
		/// </summary>
		public static DateTime FromEpoch(int time)
		{
			return spanDate.AddSeconds(time).ToLocalTime();
		}
	}
}
