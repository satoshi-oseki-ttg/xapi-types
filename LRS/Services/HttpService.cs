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
        private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

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

        public MultipartContent CreateMultipartContent(object value)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var multipartContent = new MultipartContent("mixed", $"----{nowTicks}");

            multipartContent.Add(new StringContent(
                JsonConvert.SerializeObject(value, jsonSerializerSettings), Encoding.UTF8, "application/json"));
            AddAttachments(value, multipartContent);

            return multipartContent;
        }

        private void AddAttachments(object value, MultipartContent multipartContent) // value : Statement | StatementResult
        {
            if (value is Statement)
            {
               AddAttachments(((Statement)value).Attachments, multipartContent);
            }
            if (value is StatementsResult)
            {
                var statements = ((StatementsResult)value).Statements;
                foreach (var statement in statements)
                {
                    AddAttachments(statement.Attachments, multipartContent);
                }
            }
        }

        private void AddAttachments(IList<Attachment> attachments, MultipartContent multipartContent)
        {
            if (attachments == null)
            {
                return;
            }
            
            foreach (var attachment in attachments)
            {
                if (attachment.Content == null)
                {
                    continue;
                }
                var content = new StringContent(
                    Encoding.UTF8.GetString(attachment.Content),
                    Encoding.UTF8, attachment.ContentType);
                content.Headers.Add("Content-Transfer-Encoding", "binary");
                content.Headers.Add("X-Experience-API-Hash", attachment.Sha2);
                multipartContent.Add(content);
            }
        }
    }
}
