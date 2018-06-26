using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Hosting;
using System.Web.Http.ModelBinding;
using System.Web.Http.Owin;
using System.Web.Http.ValueProviders;

namespace Kay.KWebApi
{
	public static class KExtensions
	{
		private static readonly IHostBufferPolicySelector _defaultBufferPolicySelector = new OwinBufferPolicySelector();

		public static IAppBuilder KWebApi(this IAppBuilder builder, HttpConfiguration configuration, string pathPrefix = "api", string serviceNameSuffix = "", string namespacePrefix = "", bool outputCamelCase = false)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}

			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			configuration.KWebApi(pathPrefix, serviceNameSuffix, namespacePrefix, outputCamelCase);

			HttpServer server = new HttpServer(configuration);

			try
			{
				HttpMessageHandlerOptions options = CreateOptions(builder, server, configuration);
				return UseMessageHandler(builder, options);
			}
			catch
			{
				server.Dispose();
				throw;
			}
		}

		public static void KWebApi(this HttpConfiguration configuration, string pathPrefix = "api", string serviceNameSuffix = "", string namespacePrefix = "", bool outputCamelCase = false)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			//serviceNameSuffix

			serviceNameSuffix = (serviceNameSuffix + string.Empty).Trim();
			var suffix = typeof(DefaultHttpControllerSelector).GetField("ControllerSuffix", BindingFlags.Static | BindingFlags.Public);
			if (suffix != null) suffix.SetValue(null, serviceNameSuffix);

			//namespacePrefix

			KHelper.BaseNamespace = (namespacePrefix + string.Empty).Trim(' ', '/', '.');
			
			//route
			
			pathPrefix = (pathPrefix + string.Empty).Trim(' ', '/');
			if (!string.IsNullOrWhiteSpace(pathPrefix)) pathPrefix += '/';

			configuration.EnableCors(new EnableCorsAttribute("*", "*", "*"));
			configuration.MapHttpAttributeRoutes();

			var delegationHandler = new KDelegatingHandler(configuration);
			configuration.Routes.MapHttpRoute("KWebApiWithNamespace", pathPrefix + "{namespace}/{controller}/{action}", null, null, delegationHandler);
			configuration.Routes.MapHttpRoute("KWebApiWithoutNamespace", pathPrefix + "{controller}/{action}", null, null, delegationHandler);

			configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new KHttpControllerTypeResolver(configuration));
			configuration.Services.Replace(typeof(IHttpActionSelector), new KApiControllerActionSelector(configuration));
			configuration.Services.Replace(typeof(IHttpControllerSelector), new KApiControllerSelector(configuration));
			
			//json.net

			KHelper.Config = configuration;
			KHelper.OutputCamelCase = outputCamelCase;
			configuration.Formatters.JsonFormatter.SerializerSettings = KHelper.JsonSerializerSettings;
			
			//parameter handling

			configuration.ParameterBindingRules.Insert(0, descriptor => new KHttpParameterBinding(descriptor));
		}

		private static HttpMessageHandlerOptions CreateOptions(IAppBuilder builder, HttpServer server, HttpConfiguration configuration)
		{
			Contract.Assert(builder != null);
			Contract.Assert(server != null);
			Contract.Assert(configuration != null);

			ServicesContainer services = configuration.Services;
			Contract.Assert(services != null);

			IHostBufferPolicySelector bufferPolicySelector = services.GetHostBufferPolicySelector() ?? _defaultBufferPolicySelector;
			IExceptionLogger exceptionLogger = ExceptionServices.GetLogger(services);
			IExceptionHandler exceptionHandler = ExceptionServices.GetHandler(services);

			return new HttpMessageHandlerOptions
			{
				MessageHandler = server,
				BufferPolicySelector = bufferPolicySelector,
				ExceptionLogger = exceptionLogger,
				ExceptionHandler = exceptionHandler,
				AppDisposing = builder.GetOnAppDisposingProperty()
			};
		}
		
		private static IAppBuilder UseMessageHandler(this IAppBuilder builder, HttpMessageHandlerOptions options)
		{
			Contract.Assert(builder != null);
			Contract.Assert(options != null);

			return builder.Use(typeof(HttpMessageHandlerAdapter), options);
		}

		internal static CancellationToken GetOnAppDisposingProperty(this IAppBuilder builder)
		{
			Contract.Assert(builder != null);

			IDictionary<string, object> properties = builder.Properties;

			if (properties == null)
			{
				return CancellationToken.None;
			}

			object value;

			if (!properties.TryGetValue("host.OnAppDisposing", out value))
			{
				return CancellationToken.None;
			}

			CancellationToken? token = value as CancellationToken?;

			if (!token.HasValue)
			{
				return CancellationToken.None;
			}

			return token.Value;
		}
	}
}