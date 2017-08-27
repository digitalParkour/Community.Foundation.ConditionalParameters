using System;
using System.Linq;
using System.Web;
using Sitecore.Diagnostics;     
using Sitecore.Rules.Actions; 
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Text;

namespace Community.Foundation.ConditionalParameters.Rules.Actions
{
    /// <summary>
    /// "set multilist [parameter], remove [term] (if exists)"
    /// Remove (if exists) one term of a delimited parameter value
    /// Example: remove one ID from a multilist
    /// </summary>
    public class RemoveParameterTermAction<T> : RuleAction<T>
    where T : ConditionalRenderingsRuleContext
    {
        public string Name { get; set; }        // Name of rendering param
        public string Term { get; set; }        // Term of rendering param value to remove
        public string Delimiter { get; set; }   // Example "|" ... ugh this breaks the UI

        public override void Apply(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");
            Assert.ArgumentNotNullOrEmpty(Name, "Name");
            Assert.ArgumentNotNullOrEmpty(Term, "Term");
            // Assert.ArgumentNotNullOrEmpty(Delimiter, "Delimiter");
            Delimiter = "|";

            if (string.IsNullOrWhiteSpace(ruleContext.Reference.Settings.Parameters))
            {                                                                                        
                return;
            }

            var urlString = new UrlString(ruleContext.Reference.Settings.Parameters);
            var rawValue = urlString.Parameters[Name];
            if (string.IsNullOrWhiteSpace(rawValue))
                return;
                                  
            var valueList = rawValue.Split(new string[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries);
            var newList = string.Join(Delimiter,
                valueList.Where(x => x!=null && !HttpUtility.UrlDecode(x).Equals(HttpUtility.UrlDecode(Term), StringComparison.OrdinalIgnoreCase))
                );
            
            urlString.Append(Name, newList ?? string.Empty);
            ruleContext.Reference.Settings.Parameters = urlString.GetUrl();
        }
    }
}