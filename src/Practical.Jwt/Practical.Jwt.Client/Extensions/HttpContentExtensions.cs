using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Practical.Jwt.Client.Extensions
{
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAs<T>(this HttpContent httpContent)
        {
            var content = await httpContent.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            if (httpContent.Headers.ContentType.MediaType == "application/json")
            {
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            else if (httpContent.Headers.ContentType.MediaType == "application/xml")
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                using var stringReader = new StringReader(content);
                return (T)xmlSerializer.Deserialize(stringReader);
            }

            throw new Exception($"Unsupported Media Type: {httpContent.Headers.ContentType.MediaType}");

        }

        public static StringContent AsStringContent(this object obj, string contentType)
        {
            var text = string.Empty;

            if (contentType == "application/json")
            {
                text = JsonSerializer.Serialize(obj);
            }
            else if (contentType == "application/xml")
            {
                var xmlSerializer = new XmlSerializer(obj.GetType());

                using var stringWriter = new StringWriter();
                using XmlWriter xmlWriter = XmlWriter.Create(stringWriter);
                xmlSerializer.Serialize(xmlWriter, obj);
                text = stringWriter.ToString();
            }
            else
            {
                throw new Exception($"Unsupported Media Type: {contentType}");
            }

            var content = new StringContent(text);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return content;
        }

        public static StringContent AsJsonContent(this object obj)
        {
            return obj.AsStringContent("application/json");
        }

        public static StringContent AsXmlContent(this object obj)
        {
            return obj.AsStringContent("application/xml");
        }
    }
}
