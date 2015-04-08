﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CiWong.OpenAPI.Core
{
	/// <summary>
	/// 自定义
	/// </summary>
	public class CustomJsonFormatter : JsonMediaTypeFormatter
	{
		private JsonSerializerSettings _jsonSerializerSettings;
		private Encoding encode = null;
		public CustomJsonFormatter(JsonSerializerSettings jsonSerializerSettings = null)
		{
			_jsonSerializerSettings = jsonSerializerSettings ?? JSONHelper.Settings;
			SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
			encode = new UTF8Encoding(false, true);
		}

		public override bool CanReadType(Type type)
		{
			return true;
		}

		public override bool CanWriteType(Type type)
		{
			return true;
		}

		public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, System.Net.Http.HttpContent content, IFormatterLogger formatterLogger)
		{
			JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);
			return Task.Factory.StartNew(() =>
			{
				using (StreamReader streamReader = new StreamReader(readStream, encode))
				{
					using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
					{
						return serializer.Deserialize(jsonTextReader, type);
					}
				}
			});
		}

		public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, System.Net.Http.HttpContent content, System.Net.TransportContext transportContext)
		{
			JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);
			return Task.Factory.StartNew(() =>
			{
				using (StreamWriter streamWriter = new StreamWriter(writeStream, encode))
				{
					using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
					{
						ApiResult result;
						if (null != value)
						{
							if (value is ApiException)
							{
								var apiException = (ApiException)value;
								result = new ApiResult() { Ret = apiException.Ret, ErrorCode = apiException.ErrCode, Message = apiException.Message };
							}
							else if (value is ApiResult)
							{
								result = value as ApiResult;
							}
							else if (value is HttpError)
							{
								var exception = (HttpError)value;
								string message = string.Join(",", exception.Select(t => string.Concat(t.Key, ":", t.Value)));
								result = new ApiResult() { Ret = RetEum.HttpError, ErrorCode = 1, Message = message };
							}
							else
							{
								result = new ApiResult<object>() { Data = value, Message = "success" };
							}
						}
						else
						{
							result = new ApiResult<object>() { Data = value, Message = "success" };
						}
						serializer.Serialize(jsonTextWriter, result);
					}
				}
			});
		}
	}
}