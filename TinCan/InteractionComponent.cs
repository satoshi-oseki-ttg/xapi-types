using System;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class InteractionComponent
    {
        public string id;
        public Uri Id
        {
            get
            {
                try
                {
                    return new Uri(id);
                }
                catch(Exception)
                {
                    return null;
                }
            }
            set { this.id = value.ToString(); }
        }
        public LanguageMap description { get; set; }
    }
}
