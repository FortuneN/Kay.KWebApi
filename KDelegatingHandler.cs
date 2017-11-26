using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;

namespace Kay.KWebApi
{
	public class KDelegatingHandler : DelegatingHandler
	{
		public KDelegatingHandler(HttpConfiguration httpConfiguration)
		{
			InnerHandler = new HttpControllerDispatcher(httpConfiguration);
		}

		async protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = await base.SendAsync(request, cancellationToken);

			if (response.Content != null && response.Content is ObjectContent)
			{
				var kFileInfo = default(KFileInfo);

				var objectContent = (ObjectContent)response.Content;
				
				if (objectContent.ObjectType == typeof(KFileInfo))
				{
					kFileInfo = (KFileInfo)objectContent.Value;
				}
				else if (objectContent.ObjectType == typeof(byte[]))
				{
					kFileInfo = KFileInfo.FromBytes((byte[])objectContent.Value);
				}
				else if (objectContent.ObjectType == typeof(Stream))
				{
					kFileInfo = KFileInfo.FromStream((Stream)objectContent.Value);
				}
				else if (objectContent.ObjectType == typeof(FileInfo))
				{
					kFileInfo = KFileInfo.FromFileInfo((FileInfo)objectContent.Value);
				}

				if (kFileInfo != null)
				{
					return kFileInfo.AsResponse(true);
				}
			}

			return response;
		}
	}
}