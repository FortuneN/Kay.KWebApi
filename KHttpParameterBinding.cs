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
using Newtonsoft.Json;

namespace Kay.KWebApi
{
	public class KHttpParameterBinding : HttpParameterBinding
	{
		public KHttpParameterBinding(HttpParameterDescriptor descriptor) : base(descriptor)
		{
		}

		public override async Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
		{
			if (actionContext.ActionDescriptor.GetCustomAttributes<KMethod>().Any())
			{
				var value = Descriptor.DefaultValue;

				try
				{
					ResetStream(await actionContext.Request.Content.ReadAsStreamAsync());

					// TYPE : query string

					var queryValues = actionContext.Request.GetQueryNameValuePairs().Where(x => x.Key.Equals(Descriptor.ParameterName, StringComparison.OrdinalIgnoreCase));
					if (queryValues.Any())
					{
						value = ConvertValueToParameterType(queryValues.First().Value);
					}

					// TYPE : multipart/form-data

					if (actionContext.Request.Content.IsMimeMultipartContent())
					{
						var provider = await actionContext.Request.Content.ReadAsMultipartAsync();
						try
						{
							var httpContent = provider.Contents.FirstOrDefault(hc => hc.Headers.ContentDisposition.Name.Equals(Descriptor.ParameterName, StringComparison.OrdinalIgnoreCase) || hc.Headers.ContentDisposition.Name.Equals($"\"{Descriptor.ParameterName}\"", StringComparison.OrdinalIgnoreCase));
							if (httpContent != null)
							{
								if (string.IsNullOrWhiteSpace(httpContent.Headers.ContentDisposition.FileName) && string.IsNullOrWhiteSpace(httpContent.Headers.ContentDisposition.FileNameStar))
								{
									value = ConvertValueToParameterType(await httpContent.ReadAsStringAsync());
								}
								else
								{
									if (Descriptor.ParameterType == typeof(KFileInfo))
									{
										value = KFileInfo.FromStream(await httpContent.ReadAsStreamAsync(), string.IsNullOrWhiteSpace(httpContent.Headers.ContentDisposition.FileName.Trim(' ', '"')) ? httpContent.Headers.ContentDisposition.FileNameStar.Trim(' ', '"') : httpContent.Headers.ContentDisposition.FileName.Trim(' ', '"'), httpContent.Headers.ContentType.MediaType, httpContent.Headers.ContentLength);
									}
									else if (Descriptor.ParameterType == typeof(byte[]))
									{
										value = await httpContent.ReadAsByteArrayAsync();
									}
									else if (Descriptor.ParameterType == typeof(Stream))
									{
										value = await httpContent.ReadAsStreamAsync();
									}
									else if (Descriptor.ParameterType == typeof(FileInfo))
									{
										var tempFileName = Path.GetTempFileName();
										File.WriteAllBytes(tempFileName, await httpContent.ReadAsByteArrayAsync());
										value = new FileInfo(tempFileName);
									}
									else if (Descriptor.ParameterType == typeof(string))
									{
										value = Convert.ToBase64String(await httpContent.ReadAsByteArrayAsync());
									}
								}
							}
						}
						finally
						{
							ResetStream(await actionContext.Request.Content.ReadAsStreamAsync());
						}
					}

					// TYPE : application/x-www-form-urlencode

					if (actionContext.Request.Content.IsFormData())
					{
						var formData = await actionContext.Request.Content.ReadAsFormDataAsync();
						if (formData != null && formData.AllKeys.Any(x => x.Equals(Descriptor.ParameterName, StringComparison.OrdinalIgnoreCase)))
						{
							value = ConvertValueToParameterType(formData[Descriptor.ParameterName]);
						}
					}

					// TYPE : application/json

					if (actionContext.Request.Content?.Headers?.ContentType?.MediaType?.ToLower() == "application/json")
					{
						var json = await actionContext.Request.Content.ReadAsStringAsync();
						if (!string.IsNullOrWhiteSpace(json))
						{
							var jObject = JsonConvert.DeserializeObject<JObject>(json, KHelper.JsonSerializerSettings); // we expect a json object, must blow up otherwise
							value = ConvertValueToParameterType(jObject.Properties().Where(x => x.Name.Equals(Descriptor.ParameterName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
						}
					}
				}
				catch (Exception ex)
				{
					value = Descriptor.DefaultValue;
				}
				finally
				{
					ResetStream(await actionContext.Request.Content.ReadAsStreamAsync());
					SetValue(actionContext, value);
				}
			}
		}

		private void ResetStream(Stream stream)
		{
			try { if (stream == null || stream.Position == 0) return; } catch { /*ignore*/ }
			try { stream.Position = 0; } catch { /*ignore*/ }
			try { stream.Seek(0, SeekOrigin.Begin); } catch { /*ignore*/ }
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
				if (ae.Message.Contains("Null to Guid")) return Descriptor.DefaultValue;
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
				if (ae.Message.Contains("Null to Guid")) return Descriptor.DefaultValue;
				else throw ae;
			}
		}
	}
}