using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Kay.KWebApi
{
	public class KHttpControllerTypeResolver : DefaultHttpControllerTypeResolver
	{
		public KHttpControllerTypeResolver() : base(IsHttpEndpoint) { }

		internal static bool IsHttpEndpoint(Type t)
		{
			if (t == null) throw new ArgumentNullException("t");

			return
			t.IsClass &&
			t.IsVisible &&
			!t.IsAbstract &&
			typeof(KService).IsAssignableFrom(t) && 
			typeof(IHttpController).IsAssignableFrom(t);
		}
	}
}
