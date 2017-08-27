using Sitecore.Diagnostics;     
using Sitecore.Rules.Actions; 
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Text;

namespace Community.Foundation.ConditionalParameters.Rules.Actions
{
    /// <summary>
    /// Add or update one specific parameter, maintain the rest of the parameters as is
    /// </summary>
    public class SetParameterAction<T> : RuleAction<T>
    where T : ConditionalRenderingsRuleContext
    {
        public string Name { get; set; }
        public string Value { get; set; }
               
        public override void Apply(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");
            Assert.ArgumentNotNullOrEmpty(Name, "Name");

            if (string.IsNullOrWhiteSpace(ruleContext.Reference.Settings.Parameters))
            { 
                ruleContext.Reference.Settings.Parameters = $"{Name}={Value ?? string.Empty}}}";
                return;
            }
            var urlString = new UrlString(ruleContext.Reference.Settings.Parameters);
            urlString.Append(Name,Value ?? string.Empty);
            ruleContext.Reference.Settings.Parameters = urlString.GetUrl();
        }
    }
}