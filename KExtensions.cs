using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ModelBinding;
using System.Web.Http.ValueProviders;

namespace Kay.KWebApi
{
	public static class KExtensions
	{
		public static void KWebApi(this HttpConfiguration config, string pathPrefix = "api", string serviceNameSuffix = "")
		{
			//route

			pathPrefix = (pathPrefix + string.Empty).Trim(' ', '/');
			if (!string.IsNullOrWhiteSpace(pathPrefix)) pathPrefix += '/';

			config.Routes.MapHttpRoute("KWebApi", pathPrefix + "{controller}/{action}", null, null, new KDelegatingHandler(config));
			config.Services.Replace(typeof(IHttpControllerTypeResolver), new KHttpControllerTypeResolver());
			config.EnableCors();

			//services

			serviceNameSuffix = (serviceNameSuffix + string.Empty).Trim();
			var suffix = typeof(DefaultHttpControllerSelector).GetField("ControllerSuffix", BindingFlags.Static | BindingFlags.Public);
			if (suffix != null) suffix.SetValue(null, serviceNameSuffix);

			//parameter handling

			config.ParameterBindingRules.Insert(0, descriptor => new KHttpParameterBinding(descriptor));
			
			//json.net

			config.Formatters.JsonFormatter.SerializerSettings = KHelper.JsonSerializerSettings;
		}
	}
}