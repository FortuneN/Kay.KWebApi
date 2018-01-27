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
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.Http.Dispatcher;
using System.Web.Http.ModelBinding;
using System.Web.Http.ValueProviders;

namespace Kay.KWebApi
{
	public static class KExtensions
	{
		public static void KWebApi(this HttpConfiguration config, string pathPrefix = "api", string serviceNameSuffix = "", string namespacePrefix = "", bool outputCamelCase = false)
		{
			//serviceNameSuffix

			serviceNameSuffix = (serviceNameSuffix + string.Empty).Trim();
			var suffix = typeof(DefaultHttpControllerSelector).GetField("ControllerSuffix", BindingFlags.Static | BindingFlags.Public);
			if (suffix != null) suffix.SetValue(null, serviceNameSuffix);

			//namespacePrefix

			KHelper.BaseNamespace = (namespacePrefix + string.Empty).Trim(' ', '/', '.');
			
			//route
			
			pathPrefix = (pathPrefix + string.Empty).Trim(' ', '/');
			if (!string.IsNullOrWhiteSpace(pathPrefix)) pathPrefix += '/';

			config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
			config.MapHttpAttributeRoutes();

			var delegationHandler = new KDelegatingHandler(config);
			config.Routes.MapHttpRoute("KWebApiWithNamespace", pathPrefix + "{namespace}/{controller}/{action}", null, null, delegationHandler);
			config.Routes.MapHttpRoute("KWebApiWithoutNamespace", pathPrefix + "{controller}/{action}", null, null, delegationHandler);

			config.Services.Replace(typeof(IHttpControllerTypeResolver), new KHttpControllerTypeResolver(config));
			config.Services.Replace(typeof(IHttpActionSelector), new KApiControllerActionSelector(config));
			config.Services.Replace(typeof(IHttpControllerSelector), new KApiControllerSelector(config));
			
			//json.net

			KHelper.Config = config;
			KHelper.OutputCamelCase = outputCamelCase;
			config.Formatters.JsonFormatter.SerializerSettings = KHelper.JsonSerializerSettings;
			
			//parameter handling

			config.ParameterBindingRules.Insert(0, descriptor => new KHttpParameterBinding(descriptor));
		}
	}
}