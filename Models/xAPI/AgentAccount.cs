using System;

namespace bracken_lrs.Models.xAPI
{
    public class AgentAccount
    {
        public Uri HomePage { get; set; }
        public string Name { get; set; }

        public AgentAccount(Uri homePage, string name)
        {
            HomePage = homePage;
            Name = name;
        }
    }
}
