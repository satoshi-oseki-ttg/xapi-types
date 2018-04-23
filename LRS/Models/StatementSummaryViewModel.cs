using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Model
{
    public class StatementSummaryViewModel : IViewModel
    {
        private Dictionary<string, int> tally;

        public void Add(string verb)
        {
            if (tally == null)
            {
                tally = new Dictionary<string, int>();
            }

            if (tally.ContainsKey(verb))
            {
                tally[verb]++;
            }
            else
            {
                tally.Add(verb, 1);
            }
        }

        public string ToJson()
        {
            var json = new List<JObject>();
            foreach (var o in tally)
            {
                var item = new JObject();
                item.Add("verb", o.Key);
                item.Add("count", o.Value);
                json.Add(item);
            }

            return JsonConvert.SerializeObject(json);
        }
    }
}