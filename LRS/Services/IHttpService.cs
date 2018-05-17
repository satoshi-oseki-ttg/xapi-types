using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Services
{
    public interface IHttpService
    {
        string GetETag(string[] contents);
        string GetETag(string contents);
        MultipartContent CreateMultipartContent(StatementsResult statements);
    }
}
