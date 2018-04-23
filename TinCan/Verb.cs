/*
    Copyright 2014 Rustici Software

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class Verb : JsonModel
    {
        public string id;
        public Uri Id
        {
            get { return new Uri(id); }
            set { this.id = value.ToString(); }
        }
        //??public LanguageMap display { get; set; }
        public Dictionary<string, string> display { get; set; }

        public Verb() {}

        public Verb(StringOfJSON json): this(json.toJObject()) {}

        public Verb(JObject jobj)
        {
            if (jobj["id"] != null)
            {
                Id = new Uri(jobj.Value<String>("id"));
            }
            if (jobj["display"] != null)
            {
                display = new Dictionary<string, string>();
                var d = jobj.Value<JObject>("display");
                foreach (var x in d)
                {
                    display.Add(x.Key, (string)x.Value);
                }
                //(LanguageMap)jobj.Value<JObject>("display");
                //display = jobj.Value<JObject>("display");
            }
        }

        public Verb(Uri uri)
        {
            Id = uri;
        }

        public Verb(String str)
        {
            Id = new Uri (str);
        }

        public override JObject ToJObject(TCAPIVersion version) {
            JObject result = new JObject();
            if (Id != null)
            {
                result.Add("id", Id.ToString());
            }

            if (display != null && display.Count > 0/*isEmpty()*/)
            {
                //??result.Add("display", display.ToJObject(version));
            }
            // if (display != null)
            // {
            //     result.Add("display", display);
            // }

            return result;
        }

        public static explicit operator Verb(JObject jobj)
        {
            return new Verb(jobj);
        }
    }
}
