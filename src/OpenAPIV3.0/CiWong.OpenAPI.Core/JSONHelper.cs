using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using CiWong.OpenAPI.Core.Extensions;
using Newtonsoft.Json.Serialization;

namespace CiWong.OpenAPI.Core
{  
	/// <summary>
	/// JSON帮助类
	/// </summary>
	public static class JSONHelper
	{
		private static class JsonSerializerHolder
		{
			public static JsonSerializerSettings Settings;

			static JsonSerializerHolder()
			{
				Settings = new JsonSerializerSettings();

				Settings.Converters.Add(new BigintConverter());
				Settings.Converters.Add(new DateTimeConverter());
				
				Settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

				Settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			}
		}

		public static JsonSerializerSettings Settings
		{
			get { return JsonSerializerHolder.Settings; }
		}

		#region 编码
		/// <summary>
		/// 编码
		/// </summary>
		/// <typeparam name="T">类型</typeparam>
		/// <param name="t">要转换的类型数据</param>
		/// <returns>json字符串</returns>
		public static string Encode<T>(T t)
		{
			return Encode(t, Formatting.None);
		}
		#endregion

		#region 编码
		/// <summary>
		/// 编码
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="t"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		public static string Encode<T>(T t, Formatting format)
		{
			return JsonConvert.SerializeObject(t, format, Settings);
		}
		#endregion

		#region 解码
		/// <summary>
		/// 解码
		/// </summary>
		/// <typeparam name="T">类型</typeparam>
		/// <param name="json">json字符串</param>
		/// <returns>类型数据</returns>
		public static T Decode<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json, Settings);
		}
		#endregion
	}

	#region Bigint转换成字符串
	/// <summary>
	/// Bigint类型转换处理,方便javascript识别,长整形自动转换成string
	/// </summary>
	public class BigintConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(System.Int64)
				   || objectType == typeof(System.UInt64)
				   || objectType == typeof(long?);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return 0;
			}
			else
			{
				var isNullable = objectType == typeof(long?);

				var convertible = reader.Value as IConvertible;
				if (convertible == null || string.IsNullOrWhiteSpace(convertible.ToString()))
				{
					if (isNullable)
					{
						return null;
					}
					else
					{
						return (long)0;
					}
				}

				if (objectType == typeof(System.Int64))
				{
					return convertible.ToInt64(CultureInfo.InvariantCulture);
				}
				else if (objectType == typeof(System.UInt64))
				{
					return convertible.ToUInt64(CultureInfo.InvariantCulture);
				}
				else if (isNullable)
				{
					return convertible.ToInt64(CultureInfo.InvariantCulture);
				}
				return (long)0;
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteValue("0");
			}
			else if (value is Int64 || value is UInt64)
			{
				writer.WriteValue(value.ToString());
			}
			else
			{
				throw new Exception("Expected Bigint value");
			}
		}
	}
	#endregion

	#region DateTime转时间戳
	public class DateTimeConverter : DateTimeConverterBase
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				if (objectType != typeof(DateTime?))
				{
					return default(DateTime);
				}
				return null;
			}
			if (reader.TokenType != JsonToken.Integer)
			{
				throw new Exception(String.Format("日期格式错误,got {0}.", reader.TokenType));
			}

			int ticks = 0;

			if (int.TryParse(reader.Value.ToString(), out ticks))
			{
				return DateTimeExtensions.FromEpoch(ticks);
			}
			else
			{
				throw new Exception(String.Format("Cannot convert {0} value to long", reader.Value));
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteValue("null");
			}
			else if (value is DateTime)
			{
				writer.WriteValue(((DateTime)value).Epoch());
			}
			else
			{
				throw new Exception("Expected DateTime value");
			}
		}
	}
	#endregion
}
