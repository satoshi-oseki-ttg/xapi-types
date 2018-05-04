using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

// This custom validator is for supporting the custom format fields (e.g. 'iri')
// which are declared in Schemas/formats/foramts.json.
public class CustomFormatValidator : JsonValidator
{
    public string Name { get; private set; }
    public string Pattern { get; private set; }

    public CustomFormatValidator(string name, string pattern)
    {
        Name = name;
        Pattern = pattern;
    }

    public override void Validate(JToken value, JsonValidatorContext context)
    {
        if (value.Type == JTokenType.String)
        {
            string s = value.ToString();
            var regexp = new Regex(Pattern);
            if (!regexp.IsMatch(s))
            {
                context.RaiseError($"{s} doesn't match pattern {Name}: {Pattern}");
            }
        }
    }

    public override bool CanValidate(JSchema schema)
    {
        // validator will run when a schema has one of the patterns in formats.json
        return (schema.Format == Name);
    }
}