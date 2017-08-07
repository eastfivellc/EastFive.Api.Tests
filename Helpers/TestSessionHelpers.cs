using BlackBarLabs.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Api.Tests
{
    public static class TestSessionHelpers
    {
        #region Multipart Content

        public static void AddContent(this MultipartContent multipart, string name, Guid content)
        {
            var guidContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content.ToString("N")));
            guidContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("string");
            guidContent.Headers.ContentDisposition.Name = String.Format("\"{0}\"", name);
            guidContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/text");
            multipart.Add(guidContent);
        }

        public static void AddContent(this MultipartContent multipart, string name, Stream content,
            string mediaType = "application/octet-stream")
        {
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("file");
            streamContent.Headers.ContentDisposition.Name = String.Format("\"{0}\"", name);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            multipart.Add(streamContent);
        }

        public static void AddContent(this MultipartContent multipart, string name, byte [] content,
            string mediaType = "application/octet-stream")
        {
            var byteContent = new ByteArrayContent(content);
            byteContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("file");
            byteContent.Headers.ContentDisposition.Name = String.Format("\"{0}\"", name);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            multipart.Add(byteContent);
        }

        #endregion



    }
}
