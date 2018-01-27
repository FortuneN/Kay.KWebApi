using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Kay.KWebApi
{
	public class KApiControllerSelector : DefaultHttpControllerSelector
	{
		private const string NAMESPACE = "namespace";
		private const string CONTROLLER = "controller";
		private static readonly Dictionary<string, HttpControllerDescriptor> descriptors = new Dictionary<string, HttpControllerDescriptor>();

		private HttpConfiguration configuration;
		private ICollection<Type> controllerTypes = new List<Type>();
		
		public KApiControllerSelector(HttpConfiguration configuration) : base(configuration)
        {
			var assembliesResolver = configuration.Services.GetAssembliesResolver();
			var controllersResolver = configuration.Services.GetHttpControllerTypeResolver();
			this.configuration = configuration;
			this.controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);
		}

		public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
		{
			var routeData = request.GetRouteData();
			
			var @namespace = routeData?.Values?[NAMESPACE] as string;
			if (!string.IsNullOrWhiteSpace(@namespace))
			{
				var @controller = routeData?.Values?[CONTROLLER] as string;
				if (!string.IsNullOrWhiteSpace(@controller))
				{
					var controllerFullName = $"{KHelper.BaseNamespace}.{@namespace}.{controller}".Replace($"{controller}.{controller}", controller).Trim('.', ' ');
					var controllerFullNameAlternate = controllerFullName.Replace(controller, $"{@namespace}{controller}");

					controllerFullName = controllerFullName.ToUpper();
					controllerFullNameAlternate = controllerFullNameAlternate.ToUpper();

					if (!descriptors.ContainsKey(controllerFullName) && !descriptors.ContainsKey(controllerFullNameAlternate))
					{
						var type = controllerTypes.FirstOrDefault(x => x.FullName.Equals(controllerFullName, StringComparison.OrdinalIgnoreCase) || x.FullName.Equals(controllerFullNameAlternate, StringComparison.OrdinalIgnoreCase));
						descriptors[controllerFullName] = (type == null) ? null : new HttpControllerDescriptor(configuration, controllerFullName, type);
					}

					if (descriptors[controllerFullName] != null)
					{
						return descriptors[controllerFullName];
					}
				}
			}
			
			return base.SelectController(request);
		}
	}
}
