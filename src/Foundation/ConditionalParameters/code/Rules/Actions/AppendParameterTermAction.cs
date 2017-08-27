using Sitecore.Diagnostics;     
using Sitecore.Rules.Actions; 
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Text;

namespace Community.Foundation.ConditionalParameters.Rules.Actions
{
    /// <summary>
    /// "set multilist [parameter], append [term]"
    /// Add (if not already exists) one term of a delimited parameter value
    /// Example: add one ID to a multilist
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AppendParameterTermAction<T> : RuleAction<T>
    where T : ConditionalRenderingsRuleContext
    {
        public string Name { get; set; }        // Name of rendering param
        public string Term { get; set; }        // Term of rendering param value to remove
        public string Delimiter { get; set; }   // Example "|" ... originally tried adding this as input to rule, but, oddly a single pipe as a value breaks the UI

        public override void Apply(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");
            Assert.ArgumentNotNullOrEmpty(Name, "Name");
            Assert.ArgumentNotNullOrEmpty(Term, "Term");
            // Assert.ArgumentNotNullOrEmpty(Delimiter, "Delimiter");
            Delimiter = "|"; // hard code for now

            if (string.IsNullOrWhiteSpace(ruleContext.Reference.Settings.Parameters))
            { 
                ruleContext.Reference.Settings.Parameters = $"{Name}={Term ?? string.Empty}}}";
                return;
            }                        

            var urlString = new UrlString(ruleContext.Reference.Settings.Parameters);
            var rawValue = urlString.Parameters[Name];

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                urlString.Append(Name, Term ?? string.Empty);
                return;
            }

            if (!rawValue.EndsWith(Delimiter))
                rawValue += Delimiter;
            rawValue += Term;            

            urlString.Append(Name, rawValue);
            ruleContext.Reference.Settings.Parameters = urlString.GetUrl();
        }
    }
}