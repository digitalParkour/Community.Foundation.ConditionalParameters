using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Mvc.Presentation;
using Sitecore.Rules.ConditionalRenderings;

namespace Community.Foundation.ConditionalParameters.Pipelines.CustomizeRendering
{
    public class PersonalizeParametersToo : Personalize
    {           
        protected override void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
        {
            // Begin copied from sitecore
                Assert.ArgumentNotNull(args, "args");
                Assert.ArgumentNotNull(context, "context");
                RenderingReference renderingReference = context.References.Find((RenderingReference r) => r.UniqueId == context.Reference.UniqueId);
                if (renderingReference == null)
                {
                    args.Renderer = new EmptyRenderer();
                    return;
                }
                this.ApplyChanges(args.Rendering, renderingReference);
            // End copied from sitecore

            // Apply Parameters Too
            TransferRenderingParameters(args.Rendering, renderingReference);
        }

        private static void TransferRenderingParameters(Rendering rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            rendering.Parameters = new RenderingParameters(reference.Settings.Parameters);
        }
    }
}