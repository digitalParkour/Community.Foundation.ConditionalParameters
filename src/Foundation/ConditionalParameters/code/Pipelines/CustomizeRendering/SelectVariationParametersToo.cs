using System;
using System.Linq;
using Sitecore.ContentTesting.ComponentTesting;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Mvc.Presentation;            
using Sitecore.StringExtensions;                                          

namespace Community.Foundation.ConditionalParameters.Pipelines.CustomizeRendering
{
    public class SelectVariationParametersToo : Sitecore.ContentTesting.Mvc.Pipelines.Response.CustomizeRendering.SelectVariation
    {
        protected override void ApplyVariation(CustomizeRenderingArgs args, ComponentTestContext context)
        {
            // Begin copied from sitecore
                Assert.ArgumentNotNull(args, "args");
                Assert.ArgumentNotNull(context, "context");
                ComponentTestRunner componentTestRunner = new ComponentTestRunner();
                try
                {
                    componentTestRunner.Run(context);
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    string str = (ItemUtil.IsNull(context.Component.RenderingID) ? string.Empty : context.Component.RenderingID.ToString());
                    string str1 = (args.PageContext.Item != null ? args.PageContext.Item.ID.ToString() : string.Empty);
                    Log.Warn("Failed to execute MV testing on component with id \"{0}\". Item ID:\"{1}\"".FormatWith(new object[] { str, str1 }), exception, this);
                }
                RenderingReference renderingReference = context.Components.FirstOrDefault<RenderingReference>((RenderingReference c) => c.UniqueId == context.Component.UniqueId);
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