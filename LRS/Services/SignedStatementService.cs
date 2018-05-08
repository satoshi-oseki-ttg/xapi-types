using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Services
{
    public class SignedStatementService : ISignedStatementService
    {
        public async Task<Statement> GetSignedStatementAsync(Stream body, string contentType)
        {
            var boundary = contentType.Substring(contentType.IndexOf("boundary=") + "boundary=".Length);
            //var stream = await content.ReadAsStringAsync();
            var reader = new MultipartReader(boundary, body);
            MultipartSection section;
            Statement signedStatement = null;
            string jws = null;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var sectionBody = await new StreamContent(section.Body).ReadAsStringAsync();
                if (section.ContentType == "application/json")
                {
                    signedStatement = JsonConvert.DeserializeObject<Statement>(sectionBody);
                    if (signedStatement.Attachments[0].ContentType != "application/octet-stream")
                    {
                        throw new Exception("A signed statement with a malformed signature - bad content type");
                    }
                }

                if (section.ContentType == "application/octet-stream")
                {
                    jws = sectionBody; 
                }
            }

            ValidateStatement(signedStatement, jws);

            return signedStatement;
        }
        
        private void ValidateStatement(Statement statement, string jws)
        {
            string[] parts = jws.Split('.');
            string header = parts[0];
            string payload = parts[1];
            byte[] crypto = Base64UrlDecode(parts[2]);

            string headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
            JObject headerData = JObject.Parse(headerJson);
            var algorithm = headerData["alg"].ToString();
            if (!IsAlgorithmValid(algorithm))
            {
                throw new Exception("The JWS signature MUST use an algorithm of \"RS256\", \"RS384\", or \"RS512\"");
            }

            string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            JObject payloadData = JObject.Parse(payloadJson);
        }

        private bool IsAlgorithmValid(string algorithm)
        {
            return algorithm == "RS256" || algorithm == "RS384" || algorithm == "RS512";
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 1: output += "==="; break; // Three pad chars
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }
}
