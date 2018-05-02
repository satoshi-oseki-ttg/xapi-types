using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using bracken_lrs.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;


namespace bracken_lrs.Services
{
    public class xApiValidationService : IxApiValidationService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        
        public void ValidateAgent(Agent agent)
        {
            if (IsNoIfiSet(agent))
            {
                throw new Exception("Agent doesn't have none of mbox, mbox_sha1sum, openid or account.");
            }
            ValidateAgentAndGroup(agent);
        }

        public void ValidateGroup(Group group)
        {
            ValidateAgentAndGroup(group);

            if (IsNoIfiSet(group) && group.Member == null)
            {
                throw new Exception("Anonymous group must have member property set when no IFI set.");       
            }
        }

        public void ValidateVerb(Verb verb)
        {
            if (verb.Id == null)
            {
                throw new Exception("Verb id isn't set.");                
            }

            try
            {
                new Uri(verb.Id.ToString());
            }
            catch (Exception)
            {
                throw new Exception("Verb id must be valid IRI.");
            }
        }

        private void ValidateAgentAndGroup(Agent agent)
        {
            if (agent.Mbox != null)
            {
                if (agent.Account != null)
                {
                    throw new Exception("Agent can't use mbox and account together.");
                }

                if (agent.MboxSha1Sum != null)
                {
                    throw new Exception("Agent can't use mbox and mbox_sha1sum together.");
                }

                if (agent.OpenId != null)
                {
                    throw new Exception("Agent can't use mbox and openid together.");
                }

                ValidateMbox(agent.Mbox);
            }

            if (agent.MboxSha1Sum != null)
            {
                if (agent.Account != null)
                {
                    throw new Exception("Agent can't use mbox_sha1sum and account together.");
                }

                if (agent.OpenId != null)
                {
                    throw new Exception("Agent can't use mbox_sha1sum and openid together.");
                }            
            }

            if (agent.Account != null && agent.OpenId != null)
            {
                throw new Exception("Agent can't use account and openid together.");
            }

            if (agent.OpenId != null)
            {
                ValidateOpenId(agent.OpenId);
            }

            if (agent.Account != null)
            {
                if (agent.Account.HomePage == null)
                {
                    throw new Exception("Agent account must have homePage.");
                }

                if (agent.Account.Name == null)
                {
                    throw new Exception("Agent account must have homePage.");
                }
                
                try
                {
                    new Uri(agent.Account.HomePage.ToString());
                }
                catch (Exception)
                {
                    throw new Exception("Agent homePage isn't valid URI.");
                }
            }
        }

        private void ValidateMbox(string mbox)
        {
            if (!mbox.StartsWith("mailto:"))
            {
                throw new Exception("mbox must be in the form 'mailto:email address'.");
            }

            // Check if mbox is IRI (Internationalized Resource Identifier)
            try
            {
                new Uri(mbox);
            }
            catch (Exception)
            {
                throw new Exception("mbox must be IRI in the form 'mailto:email address'.");
            }

            // Check if email address is valid
            try
            {
                var emailAddress = mbox.Substring("mailto:".Length);
                new MailAddress(emailAddress);
            }
            catch (Exception)
            {
                throw new Exception("Email address is invalid.");
            }
        }

        private void ValidateOpenId(string openId)
        {
            // Check if openId is URI
            try
            {
                new Uri(openId);
            }
            catch (Exception)
            {
                throw new Exception("openId must be URI.");
            }
        }

        private bool IsNoIfiSet(Agent agent) // IFI (Inverse Functional Identifier)
        {
            return agent.Account == null
                && agent.Mbox == null
                && agent.MboxSha1Sum == null
                && agent.OpenId == null;
        }
    }
}
