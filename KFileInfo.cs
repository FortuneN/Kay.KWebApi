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
		public long? Length { get; private set; }

		public string Path { get; private set;  }
		public string Base64 { get; private set; }
		public FileInfo FileInfo { get; private set; }
		public byte[] Bytes { get; private set; }
		public Stream Stream { get; private set; }

		private KFileInfo(KFileInfoType type, string name, string contentType, object content, long? length = null)
		{
			Content = content ?? throw new ArgumentNullException("content");
			Type = type;
			Name = string.IsNullOrWhiteSpace(name) ? "file" : name;
			ContentType = string.IsNullOrWhiteSpace(contentType) ? KHelper.GetMimeType(name) : contentType;

			switch (Type)
			{
				case KFileInfoType.Base64:
					Base64 = (string)content;
					Length = length.HasValue ? length : Base64?.Length;
					break;

				case KFileInfoType.Bytes:
					Bytes = (byte[])content;
					Length = length.HasValue ? length : Bytes?.Length;
					break;

				case KFileInfoType.FileInfo:
					FileInfo = (FileInfo)content;
					Length = length.HasValue ? length : FileInfo?.Length;
					break;

				case KFileInfoType.Path:
					Path = (string)content;
					length = length.HasValue ? length : new FileInfo(Path).Length;
					break;

				case KFileInfoType.Stream:
					Stream = (Stream)content;
					Length = length.HasValue ? length : Stream?.Length;
					break;
			}
		}

		public static KFileInfo FromPath(string path, string name = null, string contentType = null, long? length = null)
		{
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException("path");
			return new KFileInfo(KFileInfoType.Path, System.IO.Path.GetFileName(path), contentType, path, length);
		}

		public static KFileInfo FromBase64(string base64, string name = null, string contentType = null, long? length = null)
		{
			if (string.IsNullOrWhiteSpace(base64)) throw new ArgumentNullException("base64");
			return new KFileInfo(KFileInfoType.Base64, name, contentType, base64, length);
		}

		public static KFileInfo FromFileInfo(FileInfo fileInfo, string name = null, string contentType = null, long? length = null)
		{
			if (fileInfo == null) throw new ArgumentNullException("fileInfo");
			return new KFileInfo(KFileInfoType.FileInfo, System.IO.Path.GetFileName(fileInfo.FullName), contentType, fileInfo, length);
		}

		public static KFileInfo FromBytes(byte[] bytes, string name = null, string contentType = null, long? length = null)
		{
			if (bytes == null) throw new ArgumentNullException("bytes");
			return new KFileInfo(KFileInfoType.Bytes, name, contentType, bytes, length);
		}

		public static KFileInfo FromStream(Stream stream, string name = null, string contentType = null, long? length = null)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			return new KFileInfo(KFileInfoType.Stream, name, contentType, stream, length);
		}

		public Stream AsStream()
		{
			switch (Type)
			{
				case KFileInfoType.Base64: return new MemoryStream(Convert.FromBase64String(Base64));
				case KFileInfoType.Bytes: return new MemoryStream(Bytes);
				case KFileInfoType.FileInfo: return FileInfo.OpenRead();
				case KFileInfoType.Path: return new FileInfo(Path).OpenRead();
				case KFileInfoType.Stream: return Stream;
				default: throw new Exception("Unknown Type");
			}
		}
		
		public HttpResponseMessage AsResponse(bool deleteOnClose = false)
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(AsStream()) };
			response.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
			response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = $"\"{Name}\"" };
			return response;
		}

		public void Dispose()
		{
		}
	}
}