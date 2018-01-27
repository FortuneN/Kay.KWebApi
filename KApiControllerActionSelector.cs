using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Kay.KWebApi
{
	/*
	 * Feature : respond to both XXX and XXXAsync uri's
	 * Feature : case insensitivity
	 * Feature : anti-duplication
	 */
	public class KApiControllerActionSelector : ApiControllerActionSelector
	{
		private const string Action = "action";
		private const string AsyncSuffix = "Async";
		
		private static SortedDictionary<string, HttpActionDescriptor> ActionDescriptorCache = new SortedDictionary<string, HttpActionDescriptor>();
		private static string ActionKey(Type type, string name) => $"{type.FullName}:{name}".ToUpper();

		public KApiControllerActionSelector(HttpConfiguration config) : base() { }

		public override ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
		{
			try
			{
				var iLookupWithoutAsync = new SortedDictionary<string, HttpActionDescriptor>();

				foreach (var actionMethod in controllerDescriptor.ControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.IsPublic && !x.IsStatic && x.GetCustomAttribute<KMethod>() != null).ToList())
				{
					var actionNameWithAsync = actionMethod.Name.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase) ? actionMethod.Name : $"{actionMethod.Name}{AsyncSuffix}";
					var actionNameWithoutAsync = actionNameWithAsync.Substring(0, actionNameWithAsync.LastIndexOf(AsyncSuffix));

					var actionKeyWithAsync = ActionKey(controllerDescriptor.ControllerType, actionNameWithAsync);
					var actionKeyWithoutAsync = ActionKey(controllerDescriptor.ControllerType, actionNameWithoutAsync);

					/*
					if (ActionDescriptorCache.ContainsKey(actionKeyWithAsync))
					{
						throw new Exception($"Ambiguous KMethod name '{controllerDescriptor.ControllerType.FullName}.{actionKeyWithAsync}'");
					}

					if (ActionDescriptorCache.ContainsKey(actionKeyWithoutAsync))
					{
						throw new Exception($"Ambiguous KMethod name '{controllerDescriptor.ControllerType.FullName}.{actionKeyWithoutAsync}'");
					}
					*/

					var descriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, actionMethod);
					ActionDescriptorCache[actionKeyWithAsync] = descriptor;
					ActionDescriptorCache[actionKeyWithoutAsync] = descriptor;
					iLookupWithoutAsync[actionNameWithoutAsync] = descriptor;
				}

				return iLookupWithoutAsync.ToLookup(k => k.Key, v => v.Value);
			}
			catch (Exception ex)
			{
				return base.GetActionMapping(controllerDescriptor);
			}
		}

		public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
		{
			if (controllerContext.RouteData.Values.TryGetValue(Action, out object objAction))
			{
				var actionKey = ActionKey(controllerContext.ControllerDescriptor.ControllerType, (string)objAction);
				if (ActionDescriptorCache.ContainsKey(actionKey))
				{
					return ActionDescriptorCache[actionKey];
				}
			}
			return base.SelectAction(controllerContext);
		}
	}
}