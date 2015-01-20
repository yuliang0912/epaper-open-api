﻿﻿using System;
using System.IO;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

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
						if (value != null && value is ApiException)
						{
							var apiException = (ApiException)value;
							result = new ApiResult() { Ret = apiException.Ret, ErrorCode = apiException.ErrCode, Message = apiException.Message };
						}
						else if (value != null && value is HttpError)
						{
							var exception = (HttpError)value;
							result = new ApiResult() { Ret = RetEum.HttpError, ErrorCode = 1, Message = exception.Message };
						}
						else if (value != null && value is ApiResult)
						{
							result = value as ApiResult;
						}
						else
						{
							result = new ApiResult<object>() { Data = value };
						}
						serializer.Serialize(jsonTextWriter, result);
					}
				}
			});
		}
	}
}