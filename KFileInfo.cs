using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Kay.KWebApi
{
	public enum KFileInfoType
	{
		Path,
		FileInfo,
		Base64,
		Bytes,
		Stream
	}

	public class KFileInfo: IDisposable
	{
		public KFileInfoType Type { get; private set; }
		public string ContentType { get; private set; }
		public string Name { get; private set; }
		public Object Content { get; private set; }

		public string Path { get; private set;  }
		public string Base64 { get; private set; }
		public FileInfo FileInfo { get; private set; }
		public byte[] Bytes { get; private set; }
		public Stream Stream { get; private set; }

		private KFileInfo(KFileInfoType type, string name, string contentType, object content)
		{
			Type = type;
			Name = string.IsNullOrWhiteSpace(name) ? "file" : name;
			ContentType = string.IsNullOrWhiteSpace(contentType) ? KHelper.GetMimeType(System.IO.Path.GetExtension(name)) : contentType;
			Content = content;
		}

		public static KFileInfo FromPath(string path, string name = null, string contentType = null)
		{
			return new KFileInfo(KFileInfoType.Path, System.IO.Path.GetFileName(path), contentType, path) { Path = path };
		}

		public static KFileInfo FromBase64(string base64, string name = null, string contentType = null)
		{
			return new KFileInfo(KFileInfoType.Base64, name, contentType, base64) { Base64 = base64 };
		}

		public static KFileInfo FromFileInfo(FileInfo fileInfo, string name = null, string contentType = null)
		{
			return new KFileInfo(KFileInfoType.FileInfo, System.IO.Path.GetFileName(fileInfo.FullName), contentType, fileInfo) { FileInfo = fileInfo };
		}

		public static KFileInfo FromBytes(byte[] bytes, string name = null, string contentType = null)
		{
			return new KFileInfo(KFileInfoType.Bytes, name, contentType, bytes) { Bytes = bytes };
		}

		public static KFileInfo FromStream(Stream stream, string name = null, string contentType = null)
		{
			return new KFileInfo(KFileInfoType.Stream, name, contentType, stream) { Stream = stream };
		}
		
		public HttpResponseMessage AsResponse(bool deleteOnClose = false)
		{
			var stream = default(Stream);
			
			switch (Type)
			{
				case KFileInfoType.Path:
					stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, deleteOnClose ? FileOptions.DeleteOnClose : FileOptions.None);
					break;

				case KFileInfoType.FileInfo:
					stream = new FileStream(FileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, deleteOnClose ? FileOptions.DeleteOnClose : FileOptions.None);
					break;

				case KFileInfoType.Base64:
					stream = new MemoryStream(Convert.FromBase64String(Base64));
					break;

				case KFileInfoType.Bytes:
					stream = new MemoryStream(Bytes);
					break;

				case KFileInfoType.Stream:
					stream = Stream;
					break;
			}

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Content = new StreamContent(stream);
			response.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
			response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = $"\"{Name}\"" };
			return response;
		}

		public void Dispose()
		{
			var dispose = true;
		}
	}
}