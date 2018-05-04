using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using bracken_lrs.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace bracken_lrs.Services
{
    public class xApiValidationService : IxApiValidationService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private JSchema _schemas;
        
        public xApiValidationService()
        {
            JoinSchemas();
        }

        private void JoinSchemas()
        {
            var schemaBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Models/xAPI/Schemas");
            var schemaDirectory = new DirectoryInfo(schemaBasePath);
            var schemaFiles = schemaDirectory.EnumerateFiles("*.json");
            var schemas = new JObject();
            foreach (var item in schemaFiles)
            {
                using (StreamReader file = File.OpenText(item.FullName))
                {
                    string json = file.ReadToEnd();
                    var jObject = JsonConvert.DeserializeObject<JObject>(json);
                    var id = jObject["id"].Value<string>(); // #<name>
                    var name = id.Substring(1);
                    schemas.Add(name, jObject);
                }
            }

            var root = new JObject();
            root.Add("$schema", new JValue("http://json-schema.org/draft-04/schema#"));
            root.Add("additionalProperties", new JValue(false));
            root.Add("type", new JValue("object"));
            root.Add("properties", schemas);
            
            JSchemaReaderSettings settings = new JSchemaReaderSettings
            {
                Validators = GetStringFormats()
            };
            _schemas = JSchema.Parse(root.ToString(), settings);
        }

        private IList<JsonValidator> GetStringFormats()
        {
            var formatBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Models/xAPI/Schemas/formats");
            var schemaDirectory = new DirectoryInfo(formatBasePath);
            var formatFiles = schemaDirectory.EnumerateFiles("formats.json");
            var formats = new List<JsonValidator>();
            foreach (var item in formatFiles)
            {
                using (StreamReader file = File.OpenText(item.FullName))
                {
                    string json = file.ReadToEnd();
                    var jObject = JsonConvert.DeserializeObject<JObject>(json);
                    foreach (var f in jObject)
                    {
                        formats.Add(new CustomFormatValidator(f.Key, f.Value.Value<string>()));
                    }
                }
            }

            return formats;
        }

        public void ValidateStatement(JObject statement)
        {
            IList<string> errorMessages;
            bool isValid = statement.IsValid(_schemas.Properties["statement"], out errorMessages);
            if (!isValid)
            {
                var messages = new StringBuilder();
                foreach (var message in errorMessages)
                {
                    messages.AppendLine(message.ToString());
                }

                throw new Exception($@"Invalid statement.\n{messages}");
            }
        }

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
