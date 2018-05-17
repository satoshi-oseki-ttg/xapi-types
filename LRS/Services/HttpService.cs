using System;
using System.Security.Cryptography;
using System.Text;

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
    }
}
