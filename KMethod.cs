using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Kay.KWebApi
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class KMethod : Attribute, IActionHttpMethodProvider
	{
		private static readonly Collection<HttpMethod> _supportedMethods = new Collection<HttpMethod>(new HttpMethod[] { HttpMethod.Post, HttpMethod.Get });

		public Collection<HttpMethod> HttpMethods
		{
			get
			{
				return _supportedMethods;
			}
		}
	}
}
