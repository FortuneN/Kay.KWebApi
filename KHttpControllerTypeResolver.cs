using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Kay.KWebApi
{
	public class KHttpControllerTypeResolver : DefaultHttpControllerTypeResolver
	{
		public KHttpControllerTypeResolver(HttpConfiguration config) : base(type => type != null && type.IsClass && type.IsVisible && !type.IsAbstract && typeof(KService).IsAssignableFrom(type)) { }
	}
}