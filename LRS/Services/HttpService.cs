using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace bracken_lrs.Services
{
    public class HttpService : IHttpService
    {
        public string GetETag(string[] contents)
        {
            var stringBuilder = new StringBuilder();
            foreach (var content in contents)
            {
                stringBuilder.Append(content);
            }
            
            return GetETag(stringBuilder.ToString());
        }

        public string GetETag(string content)
        {
            var enc = Encoding.GetEncoding(0);

            byte[] buffer = enc.GetBytes(content);
            var sha1 = SHA1.Create();
            var hash = BitConverter.ToString(sha1.ComputeHash(buffer)).Replace("-", "");

            return "\"" + hash + "\"";
        }

        public MultipartContent CreateMultipartContent(StatementsResult statements)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var multipartContent = new MultipartContent("mixed", $"----{nowTicks}");
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            multipartContent.Add(new StringContent(
                JsonConvert.SerializeObject(statements, jsonSerializerSettings), Encoding.UTF8, "application/json"));

            return multipartContent;
        }
    }
}
