using System;
using System.Collections.Generic;

namespace bracken_lrs.Models.xAPI
{
    public class InteractionComponent
    {
        private string id;
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
        public Dictionary<string, string> Description { get; set; }
    }
}
