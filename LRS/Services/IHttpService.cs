using System;
using System.IO;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Services
{
    public interface IHttpService
    {
        string GetETag(string[] contents);
        string GetETag(string contents);
    }
}
