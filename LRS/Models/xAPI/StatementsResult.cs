using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class StatementsResult
    {
        public IList<Statement> Statements { get; set; }
        public String More { get; set; }
        public StatementsResult(IList<Statement> statements)
        {
            Statements = statements;
        }
    }
}