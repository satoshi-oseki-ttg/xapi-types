using System;
using System.Collections.Generic;
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
    public class MultipartStatementService : IMultipartStatementService
    {
        private readonly Uri signatureUsageType = new Uri("http://adlnet.gov/expapi/attachments/signature");
        private const string octetStreamContentType = "application/octet-stream";
        private const string apiHashHeader = "X-Experience-API-Hash";

        public async Task<Statement> GetMultipartStatementAsync(Stream body, string contentType)
        {
            // Keep body content here in case there are no boundaries - see the catch section below.
            string bodyContent = "";
            using (var streamCopy = new MemoryStream())
            {
                body.CopyTo(streamCopy);
                streamCopy.Position = 0; // rewind
                bodyContent = await new StreamContent(streamCopy).ReadAsStringAsync();
                streamCopy.Position = 0; // rewind again
                body = streamCopy; 

                var boundary = contentType.Substring(contentType.IndexOf("boundary=") + "boundary=".Length);
                //var stream = await content.ReadAsStringAsync();
                var reader = new MultipartReader(boundary, body);
                MultipartSection section;
                Statement statement = null;
                string jws = null;
                bool isBinary = false;
                bool hasStatement = false;
                try
                {
                    var numStatements = 0;
                    var numAttachments = 0;
                    var attachmentHashes = new List<string>();
                    while ((section = await reader.ReadNextSectionAsync()) != null)
                    {
                        using (var streamContent = new StreamContent(section.Body))
                        {
                            var sectionBody = await streamContent.ReadAsStringAsync();
                            if (section.ContentType == "application/json") // Statement JSON
                            {
                                hasStatement = true;
                                numStatements++;
                                statement = JsonConvert.DeserializeObject<Statement>(sectionBody);
                                CheckAttachmentContentType(statement.Attachments);
                            }
                            else
                            {
                                attachmentHashes.Add(section.Headers[apiHashHeader]);
                                statement.Attachments[numAttachments++].Content = Encoding.UTF8.GetBytes(sectionBody);
                            }

                            if (section.Headers.ContainsKey("Content-Transfer-Encoding")
                                && section.Headers["Content-Transfer-Encoding"] == "binary")
                            {
                                isBinary = true;

                                if (contentType.StartsWith("multipart/form-data"))
                                {
                                    throw new Exception("A statement should fail when attachment is raw data and request content-type is multipart/form-data.");
                                }
                            }
                            if (section.ContentType == "application/octet-stream")
                            {
                                jws = sectionBody; 
                            }
                        }
                    }

                    if (numAttachments > statement?.Attachments.Count)
                    {
                        throw new Exception("Passing statement has excess multipart sections.");
                    }

                    if (numAttachments < statement?.Attachments.Count)
                    {
                        throw new Exception("Passing statement is missing multipart sections.");
                    }

                    if (!hasStatement)
                    {
                        throw new Exception("There's no statement - possibly a boundary is missing.");
                    }

                    if (numStatements > 1)
                    {
                        throw new Exception("Statements are separated into multiple parts.");
                    }

                    CheckAttachementHashes(statement.Attachments, attachmentHashes);

                    ValidateStatement(statement, jws, isBinary);
                }
                catch (Exception e) // Possibly there's only one section without a boundary.
                {
                    statement = JsonConvert.DeserializeObject<Statement>(bodyContent);
                    if (statement != null
                        && contentType.StartsWith("multipart/form-data")
                        && statement.Attachments[0].FileUrl != null)
                    {
                        throw new Exception("A statement should fail when attachment uses fileUrl and request content-type is multipart/form-data.");
                    }
                    else
                    {
                        throw e;
                    }
                }

                return statement;
            }
        }
        
        private void CheckAttachementHashes(IList<Attachment> attachments, IList<string> hashes)
        {
            if (hashes.Contains(null))
            {
                throw new Exception("Attachments missing header X-Experience-API-Hash.");
            }

            var i = 0;
            foreach (var attachment in attachments)
            {
                if (attachment.Sha2 != hashes[i])
                {
                    throw new Exception("Attachments header X-Experience-API-Hash does not match sha2.");
                }
                i++;
            }
        }

        private void CheckAttachmentContentType(IList<Attachment> attachments)
        {
            if (attachments == null)
            {
                return;
            }

            foreach (var attachment in attachments)
            {
                if (attachment.UsageType == signatureUsageType
                    && attachment.ContentType != octetStreamContentType)
                {
                    throw new Exception($"A signed statement with a malformed signature - bad content type: {attachment.ContentType}");
                }
            }
        }
        
        private void ValidateStatement(Statement statement, string jws, bool isBinary)
        {
            if (isBinary && jws == null)
            {
                return;
            }

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
