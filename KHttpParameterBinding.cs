using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Linq;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System;

namespace Kay.KWebApi
{
	public class KHttpParameterBinding : HttpParameterBinding
	{
		public KHttpParameterBinding(HttpParameterDescriptor descriptor) : base(descriptor)
		{
		}

		public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
		{
			if (actionContext.ActionDescriptor.GetCustomAttributes<KMethod>().Any())
			{
				var value = Descriptor.DefaultValue;

				try
				{
					ResetStream(actionContext.Request.Content.ReadAsStreamAsync().Result);

					// TYPE : query string

					var queryValues = actionContext.Request.GetQueryNameValuePairs().Where(x => x.Key.Equals(Descriptor.ParameterName));
					if (queryValues.Any())
					{
						value = ConvertValueToParameterType(queryValues.First().Value);
					}

					// TYPE : multipart/form-data

					if (actionContext.Request.Content.IsMimeMultipartContent())
					{
						var provider = actionContext.Request.Content.ReadAsMultipartAsync().Result;
						try
						{
							var httpContent = provider.Contents.FirstOrDefault(hc => hc.Headers.ContentDisposition.Name.Equals(Descriptor.ParameterName) || hc.Headers.ContentDisposition.Name.Equals($"\"{Descriptor.ParameterName}\""));
							if (httpContent != null)
							{
								if (string.IsNullOrWhiteSpace(httpContent.Headers.ContentDisposition.FileName) && string.IsNullOrWhiteSpace(httpContent.Headers.ContentDisposition.FileNameStar))
								{
									value = ConvertValueToParameterType(httpContent.ReadAsStringAsync().Result);
								}
								else
								{
									if (Descriptor.ParameterType == typeof(KFileInfo))
									{
										value = KFileInfo.FromStream(httpContent.ReadAsStreamAsync().Result);
									}
									else if (Descriptor.ParameterType == typeof(byte[]))
									{
										value = httpContent.ReadAsByteArrayAsync().Result;
									}
									else if (Descriptor.ParameterType == typeof(Stream))
									{
										value = httpContent.ReadAsStreamAsync().Result;
									}
									else if (Descriptor.ParameterType == typeof(FileInfo))
									{
										var tempFileName = Path.GetTempFileName();
										File.WriteAllBytes(tempFileName, httpContent.ReadAsByteArrayAsync().Result);
										value = new FileInfo(tempFileName);
									}
									else if (Descriptor.ParameterType == typeof(string))
									{
										value = Convert.ToBase64String(httpContent.ReadAsByteArrayAsync().Result);
									}
								}
							}
						}
						finally
						{
							ResetStream(actionContext.Request.Content.ReadAsStreamAsync().Result);
						}
					}

					// TYPE : application/x-www-form-urlencode

					if (actionContext.Request.Content.IsFormData())
					{
						var formData = actionContext.Request.Content.ReadAsFormDataAsync().Result;
						if (formData != null && formData.AllKeys.Any(x => x.Equals(Descriptor.ParameterName)))
						{
							value = ConvertValueToParameterType(formData[Descriptor.ParameterName]);
						}
					}

					// TYPE : application/json

					if (actionContext.Request.Content?.Headers?.ContentType?.MediaType?.ToLower() == "application/json")
					{
						var json = actionContext.Request.Content.ReadAsStringAsync().Result;
						if (!string.IsNullOrWhiteSpace(json))
						{
							var jObject = JObject.Parse(json); // we expect a json object, must blow up otherwise
							value = ConvertValueToParameterType(jObject.Property(Descriptor.ParameterName));
						}
					}
				}
				catch
				{
					value = Descriptor.DefaultValue;
				}
				finally
				{
					ResetStream(actionContext.Request.Content.ReadAsStreamAsync().Result);
					SetValue(actionContext, value);
				}
			}

			// return completed task

			var tcs = new TaskCompletionSource<object>();
			tcs.SetResult(null);
			return tcs.Task;
		}

		private void ResetStream(Stream stream)
		{
			var actionDescriptor = Descriptor.ActionDescriptor;
			if (stream == null || stream.Position == 0) return;
			stream.Seek(0, SeekOrigin.Begin);
		}

		private object ConvertValueToParameterType(string value)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(value)) return Descriptor.DefaultValue;
				return ConvertValueToParameterType(new JProperty(Descriptor.ParameterName, value));
			}
			catch (ArgumentException ae)
			{
				if (ae.Message.Contains("Can not convert Null to Guid")) return Descriptor.DefaultValue;
				throw ae;
			}
		}

		private object ConvertValueToParameterType(JProperty property)
		{
			try
			{
				if (property == null || property.Type == JTokenType.Null) return Descriptor.DefaultValue;
				return property.Value.ToObject(Descriptor.ParameterType, KHelper.JsonSerializer);
			}
			catch (ArgumentException ae)
			{
				if (ae.Message.Contains("Can not convert Null to Guid")) return Descriptor.DefaultValue;
				else throw ae;
			}
		}
	}
}