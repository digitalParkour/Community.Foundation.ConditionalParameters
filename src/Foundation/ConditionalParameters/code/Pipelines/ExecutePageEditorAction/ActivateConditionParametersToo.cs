using System.Collections.Generic;
using System.Linq;  
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;             
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Rules.Conditions;
using Sitecore.Sites;

namespace Community.Foundation.ConditionalParameters.Pipelines.ExecutePageEditorAction
{
    public class ActivateConditionParametersToo : Sitecore.Analytics.Pipelines.ExecutePageEditorAction.ActivateCondition
    {
        protected override RenderingReference DoActivateCondition(RenderingDefinition rendering, ID conditionID, Language lang, Database database, Item item, SiteContext site)
        {
            // Copied from Sitecore
                Assert.ArgumentNotNull(rendering, "rendering");
                Assert.ArgumentNotNull(conditionID, "conditionID");
                Assert.ArgumentNotNull(lang, "lang");
                Assert.ArgumentNotNull(database, "database");
                Assert.ArgumentNotNull(item, "item");
                Assert.ArgumentNotNull(site, "site");
                RenderingReference renderingReference = new RenderingReference(rendering, lang, database);
                RuleList<ConditionalRenderingsRuleContext> rules = renderingReference.Settings.Rules;
                Rule<ConditionalRenderingsRuleContext> rule = rules.Rules.FirstOrDefault<Rule<ConditionalRenderingsRuleContext>>((Rule<ConditionalRenderingsRuleContext> r) => r.UniqueId == conditionID);
                if (rule == null)
                {
                    return renderingReference;
                }
                List<RenderingReference> renderingReferences = new List<RenderingReference>()
                {
                    renderingReference
                };
                ConditionalRenderingsRuleContext conditionalRenderingsRuleContext = new ConditionalRenderingsRuleContext(renderingReferences, renderingReference)
                {
                    Item = item
                };
                rule.SetCondition(new TrueCondition<ConditionalRenderingsRuleContext>());
                rules = new RuleList<ConditionalRenderingsRuleContext>(rule);
                using (SiteContextSwitcher siteContextSwitcher = new SiteContextSwitcher(site))
                {
                    using (DatabaseSwitcher databaseSwitcher = new DatabaseSwitcher(item.Database))
                    {
                        rules.Run(conditionalRenderingsRuleContext);
                    }
                }
                Assert.IsTrue(conditionalRenderingsRuleContext.References.Count == 1, "The references count should equal 1");
                RenderingReference renderingReference1 = conditionalRenderingsRuleContext.References[0];
                Assert.IsNotNull(renderingReference1, "result");
                rendering.Datasource = renderingReference1.Settings.DataSource;
                if (renderingReference1.RenderingItem != null)
                {
                    rendering.ItemID = renderingReference1.RenderingItem.ID.ToString();
                }
            // End copied from Sitecore

            // Apply Parameters Too
            rendering.Parameters = renderingReference1.Settings.Parameters;

            return renderingReference1;
        }
    }
}