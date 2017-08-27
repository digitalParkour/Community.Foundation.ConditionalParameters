using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Pipelines.RenderRulePlaceholder;
using Sitecore.Resources;
using Sitecore.Rules;
using Sitecore.Shell.Applications.Dialogs.ItemLister;
using Sitecore.Shell.Applications.Dialogs.Personalize;
using Sitecore.Shell.Applications.Dialogs.RulesEditor;
using Sitecore.Shell.Applications.Rules;
using Sitecore.Shell.Controls;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml.Linq;
// This is copied from Sitecore.Shell.Applications.WebEdit.Dialogs.Personalization with a couple exceptions:
// 1. Changed the behavior of the "Edit rule" click event to launch the Rule Editor showing the actions (commented out line ~288 [hideActions = true])
// 2. Opted to show the actions in the personalization dialog (commented out line ~354 [skipActions = true])
namespace Community.Foundation.ConditionalParameters.Dialogs
{
    public class PersonalizationFormWithActions : DialogForm
    {    
        public const string AfterActionPlaceholderName = "afterAction";

        protected readonly string ConditionDescriptionDefault;

        [Obsolete("Use ConditionDescriptionDefault instead")]
        protected readonly static string DefaultConditionDescription;

        protected readonly static string DefaultConditionId;

        protected readonly string ConditionNameDefault;

        [Obsolete("Use ConditionNameDefault instead")]
        protected readonly static string DefaultConditionName;

        protected Checkbox ComponentPersonalization;

        protected Scrollbox RulesContainer;

        private string HideRenderingActionId = RuleIds.HideRenderingActionId.ToString();

        private string SetDatasourceActionId = RuleIds.SetDatasourceActionId.ToString();

        private string SetRenderingActionId = RuleIds.SetRenderingActionId.ToString();

        private readonly string newConditionName = Translate.Text("Specify name...");

        private readonly XElement defaultCondition;

        public Item ContextItem
        {
            get
            {
                ItemUri itemUri = ItemUri.Parse(this.ContextItemUri);
                if (itemUri == null)
                {
                    return null;
                }
                return Database.GetItem(itemUri);
            }
        }

        public string ContextItemUri
        {
            get
            {
                return base.ServerProperties["ContextItemUri"] as string;
            }
            set
            {
                base.ServerProperties["ContextItemUri"] = value;
            }
        }

        public string DeviceId
        {
            get
            {
                return Assert.ResultNotNull<string>(base.ServerProperties["deviceId"] as string);
            }
            set
            {
                Assert.IsNotNullOrEmpty(value, "value");
                base.ServerProperties["deviceId"] = value;
            }
        }

        public string Layout
        {
            get
            {
                return Assert.ResultNotNull<string>(WebUtil.GetSessionString(this.SessionHandle));
            }
        }

        public LayoutDefinition LayoutDefition
        {
            get
            {
                return LayoutDefinition.Parse(this.Layout);
            }
        }

        public string ReferenceId
        {
            get
            {
                return Assert.ResultNotNull<string>(base.ServerProperties["referenceId"] as string);
            }
            set
            {
                Assert.IsNotNullOrEmpty(value, "value");
                base.ServerProperties["referenceId"] = value;
            }
        }

        public RenderingDefinition RenderingDefition
        {
            get
            {
                return Assert.ResultNotNull<RenderingDefinition>(this.LayoutDefition.GetDevice(this.DeviceId).GetRenderingByUniqueId(this.ReferenceId));
            }
        }

        public XElement RulesSet
        {
            get
            {
                string item = base.ServerProperties["ruleSet"] as string;
                if (!string.IsNullOrEmpty(item))
                {
                    return XElement.Parse(item);
                }
                return new XElement("ruleset", this.defaultCondition);
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.ServerProperties["ruleSet"] = value.ToString();
            }
        }

        public string SessionHandle
        {
            get
            {
                return Assert.ResultNotNull<string>(base.ServerProperties["SessionHandle"] as string);
            }
            set
            {
                Assert.IsNotNullOrEmpty(value, "session handle");
                base.ServerProperties["SessionHandle"] = value;
            }
        }

        static PersonalizationFormWithActions()
        {
            PersonalizationFormWithActions.DefaultConditionDescription = Translate.Text("If none of the other conditions are true, the default condition is used.");
            PersonalizationFormWithActions.DefaultConditionId = ItemIDs.Analytics.DefaultCondition.ToString();
            PersonalizationFormWithActions.DefaultConditionName = Translate.Text("Default");
        }

        public PersonalizationFormWithActions()
        {
            this.ConditionDescriptionDefault = Translate.Text("If none of the other conditions are true, the default condition is used.");
            this.ConditionNameDefault = Translate.Text("Default");
            this.newConditionName = Translate.Text("Specify name...");
            object[] defaultConditionId = new object[] { PersonalizationFormWithActions.DefaultConditionId, this.ConditionNameDefault, RuleIds.TrueConditionId, ID.NewID.ToShortID() };
            this.defaultCondition = XElement.Parse(string.Format("<rule uid=\"{0}\" name=\"{1}\"><conditions><condition id=\"{2}\" uid=\"{3}\" /></conditions><actions /></rule>", defaultConditionId));
        }

        private static XElement AddAction(XElement rule, string id)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(id, "id");
            XName xName = "action";
            object[] xAttribute = new object[] { new XAttribute("id", id), new XAttribute("uid", ID.NewID.ToShortID()) };
            XElement xElement = new XElement(xName, xAttribute);
            XElement xElement1 = rule.Element("actions");
            if (xElement1 != null)
            {
                xElement1.Add(xElement);
            }
            else
            {
                rule.Add(new XElement("actions", xElement));
            }
            return xElement;
        }

        protected void ComponentPersonalizationClick()
        {
            if (this.ComponentPersonalization.Checked || !this.PersonalizeComponentActionExists())
            {
                SheerResponse.Eval("scTogglePersonalizeComponentSection()");
                return;
            }
            NameValueCollection nameValueCollection = new NameValueCollection();
            Context.ClientPage.Start(this, "ShowConfirm", nameValueCollection);
        }

        protected void DeleteRuleClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            string str = ID.Decode(id).ToString();
            XElement rulesSet = this.RulesSet;
            XElement xElement = (
                from node in rulesSet.Elements("rule")
                where node.GetAttributeValue("uid") == str
                select node).FirstOrDefault<XElement>();
            if (xElement != null)
            {
                xElement.Remove();
                this.RulesSet = rulesSet;
                SheerResponse.Remove(string.Concat(id, "data"));
                SheerResponse.Remove(id);
            }
        }

        protected void EditCondition(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (string.IsNullOrEmpty(args.Parameters["id"]))
            {
                SheerResponse.Alert("Please select a rule", new string[0]);
                return;
            }
            string str = ID.Decode(args.Parameters["id"]).ToString();
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    string result = args.Result;
                    XElement xElement = XElement.Parse(result).Element("rule");
                    XElement rulesSet = this.RulesSet;
                    if (xElement != null)
                    {
                        XElement xElement1 = (
                            from node in rulesSet.Elements("rule")
                            where node.GetAttributeValue("uid") == str
                            select node).FirstOrDefault<XElement>();
                        if (xElement1 != null)
                        {
                            xElement1.ReplaceWith(xElement);
                            this.RulesSet = rulesSet;
                            SheerResponse.SetInnerHtml(string.Concat(args.Parameters["id"], "_rule"), PersonalizationFormWithActions.GetRuleConditionsHtml(xElement));
                        }
                    }
                }
                return;
            }
            RulesEditorOptions rulesEditorOption = new RulesEditorOptions()
            {
                IncludeCommon = true,
                RulesPath = "/sitecore/system/settings/Rules/Conditional Renderings",
                AllowMultiple = false
            };
            RulesEditorOptions rulesEditorOption1 = rulesEditorOption;
            XElement xElement2 = (
                from node in this.RulesSet.Elements("rule")
                where node.GetAttributeValue("uid") == str
                select node).FirstOrDefault<XElement>();
            if (xElement2 != null)
            {
                rulesEditorOption1.Value = string.Concat("<ruleset>", xElement2, "</ruleset>");
            }
            // rulesEditorOption1.HideActions = true;  // FIRST CHANGE
            SheerResponse.ShowModalDialog(rulesEditorOption1.ToUrlString().ToString(), "580px", "712px", string.Empty, true);
            args.WaitForPostBack();
        }

        protected void EditConditionClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection["id"] = id;
            Context.ClientPage.Start(this, "EditCondition", nameValueCollection);
        }

        private static XElement GetActionById(XElement rule, string id)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(id, "id");
            XElement xElement = rule.Element("actions");
            if (xElement == null)
            {
                return null;
            }
            return xElement.Elements("action").FirstOrDefault<XElement>((XElement action) => action.GetAttributeValue("id") == id);
        }

        private Menu GetActionsMenu(string id)
        {
            Assert.IsNotNullOrEmpty(id, "id");
            Menu menu = new Menu()
            {
                ID = string.Concat(id, "_menu")
            };
            string themedImageSource = Images.GetThemedImageSource("office/16x16/delete.png");
            object[] objArray = new object[] { id };
            string str = "javascript:Sitecore.CollapsiblePanel.remove(this, event, \"{0}\")".FormatWith(objArray);
            menu.Add("Delete", themedImageSource, str);
            themedImageSource = string.Empty;
            object[] objArray1 = new object[] { id };
            str = "javascript:Sitecore.CollapsiblePanel.renameAction(\"{0}\")".FormatWith(objArray1);
            menu.Add("Rename", themedImageSource, str);
            menu.AddDivider().ID = "moveDivider";
            themedImageSource = Images.GetThemedImageSource("ApplicationsV2/16x16/navigate_up.png");
            object[] objArray2 = new object[] { id };
            str = "javascript:Sitecore.CollapsiblePanel.moveUp(this, event, \"{0}\")".FormatWith(objArray2);
            menu.Add("Move up", themedImageSource, str).ID = "moveUp";
            themedImageSource = Images.GetThemedImageSource("ApplicationsV2/16x16/navigate_down.png");
            object[] objArray3 = new object[] { id };
            str = "javascript:Sitecore.CollapsiblePanel.moveDown(this, event, \"{0}\")".FormatWith(objArray3);
            menu.Add("Move down", themedImageSource, str).ID = "moveDown";
            return menu;
        }

        private static XElement GetRuleById(XElement ruleSet, string id)
        {
            Assert.ArgumentNotNull(ruleSet, "ruleSet");
            Assert.ArgumentNotNull(id, "id");
            string str = ID.Parse(id).ToString();
            return ruleSet.Elements("rule").FirstOrDefault<XElement>((XElement rule) => rule.GetAttributeValue("uid") == str);
        }

        private static string GetRuleConditionsHtml(XElement rule)
        {
            Assert.ArgumentNotNull(rule, "rule");
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
            RulesRenderer rulesRenderer = new RulesRenderer(string.Concat("<ruleset>", rule, "</ruleset>"))
            {
                // SkipActions = true    // SECOND CHANGE
            };
            rulesRenderer.Render(htmlTextWriter);
            return htmlTextWriter.InnerWriter.ToString();
        }

        private string GetRuleSectionHtml(XElement rule)
        {
            Assert.ArgumentNotNull(rule, "rule");
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
            string str = ShortID.Parse(rule.GetAttributeValue("uid")).ToString();
            htmlTextWriter.Write("<table id='{ID}_body' cellspacing='0' cellpadding='0' class='rule-body'>");
            htmlTextWriter.Write("<tbody>");
            htmlTextWriter.Write("<tr>");
            htmlTextWriter.Write("<td class='left-column'>");
            this.RenderRuleConditions(rule, htmlTextWriter);
            htmlTextWriter.Write("</td>");
            htmlTextWriter.Write("<td class='right-column'>");
            this.RenderRuleActions(rule, htmlTextWriter);
            htmlTextWriter.Write("</td>");
            htmlTextWriter.Write(this.RenderRulePlaceholder("afterAction", rule));
            htmlTextWriter.Write("</tr>");
            htmlTextWriter.Write("</tbody>");
            htmlTextWriter.Write("</table>");
            string str1 = htmlTextWriter.InnerWriter.ToString().Replace("{ID}", str);
            bool flag = PersonalizationFormWithActions.IsDefaultCondition(rule);
            CollapsiblePanelRenderer.ActionsContext actionsMenu = new CollapsiblePanelRenderer.ActionsContext()
            {
                IsVisible = !flag
            };
            if (!flag)
            {
                actionsMenu.OnActionClick = "javascript:return Sitecore.CollapsiblePanel.showActionsMenu(this,event)";
                actionsMenu.Menu = this.GetActionsMenu(str);
            }
            string attributeValue = "Default";
            if (!flag || !string.IsNullOrEmpty(rule.GetAttributeValue("name")))
            {
                attributeValue = rule.GetAttributeValue("name");
            }
            CollapsiblePanelRenderer.NameContext nameContext = new CollapsiblePanelRenderer.NameContext(attributeValue)
            {
                Editable = !flag,
                OnNameChanged = "javascript:return Sitecore.CollapsiblePanel.renameComplete(this,event)"
            };
            CollapsiblePanelRenderer.NameContext nameContext1 = nameContext;
            CollapsiblePanelRenderer collapsiblePanelRenderer = new CollapsiblePanelRenderer()
            {
                CssClass = "rule-container"
            };
            return collapsiblePanelRenderer.Render(str, str1, nameContext1, actionsMenu);
        }

        private bool IsComponentDisplayed(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(this.RulesSet, id);
            if (ruleById != null && !this.IsComponentDisplayed(ruleById))
            {
                return false;
            }
            return true;
        }

        private bool IsComponentDisplayed(XElement rule)
        {
            Assert.ArgumentNotNull(rule, "rule");
            if (PersonalizationFormWithActions.GetActionById(rule, this.HideRenderingActionId) != null)
            {
                return false;
            }
            return true;
        }

        private static bool IsDefaultCondition(XElement node)
        {
            Assert.ArgumentNotNull(node, "node");
            return node.GetAttributeValue("uid") == PersonalizationFormWithActions.DefaultConditionId;
        }

        protected void MoveConditionAfter(string id, string targetId)
        {
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(targetId, "targetId");
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, id);
            XElement xElement = PersonalizationFormWithActions.GetRuleById(rulesSet, targetId);
            if (ruleById != null && xElement != null)
            {
                ruleById.Remove();
                xElement.AddAfterSelf(ruleById);
                this.RulesSet = rulesSet;
            }
        }

        protected void MoveConditionBefore(string id, string targetId)
        {
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(targetId, "targetId");
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, id);
            XElement xElement = PersonalizationFormWithActions.GetRuleById(rulesSet, targetId);
            if (ruleById != null && xElement != null)
            {
                ruleById.Remove();
                xElement.AddBeforeSelf(ruleById);
                this.RulesSet = rulesSet;
            }
        }

        protected void NewConditionClick()
        {
            XElement xElement = new XElement("rule");
            xElement.SetAttributeValue("name", this.newConditionName);
            ID newID = ID.NewID;
            xElement.SetAttributeValue("uid", newID);
            XElement rulesSet = this.RulesSet;
            rulesSet.AddFirst(xElement);
            this.RulesSet = rulesSet;
            string ruleSectionHtml = this.GetRuleSectionHtml(xElement);
            SheerResponse.Insert("non-default-container", "afterBegin", ruleSectionHtml);
            SheerResponse.Eval(string.Concat("Sitecore.CollapsiblePanel.addNew(\"", newID.ToShortID(), "\")"));
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                SheerResponse.Eval("Sitecore.CollapsiblePanel.collapseMenus()");
                return;
            }
            PersonalizeOptions personalizeOption = PersonalizeOptions.Parse();
            this.DeviceId = personalizeOption.DeviceId;
            this.ReferenceId = personalizeOption.RenderingUniqueId;
            this.SessionHandle = personalizeOption.SessionHandle;
            this.ContextItemUri = personalizeOption.ContextItemUri;
            XElement rules = this.RenderingDefition.Rules;
            if (rules != null)
            {
                this.RulesSet = rules;
            }
            if (this.PersonalizeComponentActionExists())
            {
                this.ComponentPersonalization.Checked = true;
            }
            this.RenderRules();
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            SheerResponse.SetDialogValue(this.RulesSet.ToString());
            base.OnOK(sender, args);
        }

        private bool PersonalizeComponentActionExists()
        {
            XElement rulesSet = this.RulesSet;
            return rulesSet.Elements("rule").Any<XElement>((XElement rule) => PersonalizationFormWithActions.GetActionById(rule, this.SetRenderingActionId) != null);
        }

        private static void RemoveAction(XElement rule, string id)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(id, "id");
            XElement actionById = PersonalizationFormWithActions.GetActionById(rule, id);
            if (actionById == null)
            {
                return;
            }
            actionById.Remove();
        }

        [HandleMessage("rule:rename")]
        protected void RenameRuleClick(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string item = message.Arguments["ruleId"];
            string str = message.Arguments["name"];
            Assert.IsNotNull(item, "id");
            if (!string.IsNullOrEmpty(str))
            {
                XElement rulesSet = this.RulesSet;
                XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, item);
                if (ruleById != null)
                {
                    ruleById.SetAttributeValue("name", str);
                    this.RulesSet = rulesSet;
                }
            }
        }

        private void RenderHideRenderingAction(HtmlTextWriter writer, string translatedText, bool isSelected, int index, string style)
        {
            Assert.ArgumentNotNull(writer, "writer");
            string str = string.Concat("hiderenderingaction_{ID}_", index.ToString(CultureInfo.InvariantCulture));
            writer.Write(string.Concat("<input id='", str, "' type='radio' name='hiderenderingaction_{ID}' onfocus='this.blur();' onchange=\"javascript:if (this.checked) { scSwitchRendering(this, event, '{ID}'); }\" "));
            if (isSelected)
            {
                writer.Write(" checked='checked' ");
            }
            if (!string.IsNullOrEmpty(style))
            {
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;
                object[] objArray = new object[] { style };
                writer.Write(string.Format(invariantCulture, " style='{0}' ", objArray));
            }
            writer.Write("/>");
            writer.Write(string.Concat("<label for='", str, "' class='section-header'>"));
            writer.Write(translatedText);
            writer.Write("</label>");
        }

        private void RenderPicker(HtmlTextWriter writer, Item item, string clickCommand, string resetCommand, bool prependEllipsis, bool notSet = false)
        {
            Assert.ArgumentNotNull(writer, "writer");
            Assert.ArgumentNotNull(clickCommand, "clickCommand");
            Assert.ArgumentNotNull(resetCommand, "resetCommand");
            string themedImageSource = Images.GetThemedImageSource((item != null ? item.Appearance.Icon : string.Empty), ImageDimension.id16x16);
            string str = string.Concat(clickCommand, "(\\\"{ID}\\\")");
            string str1 = string.Concat(resetCommand, "(\\\"{ID}\\\")");
            string str2 = Translate.Text("[Not set]");
            string str3 = "item-picker";
            if (item != null)
            {
                if (!notSet)
                {
                    str2 = (prependEllipsis ? ".../" : string.Empty);
                    str2 = string.Concat(str2, item.GetUIDisplayName());
                }
                else
                {
                    str2 = string.Concat(str2, (prependEllipsis ? ".../" : string.Empty));
                    str2 = string.Concat(str2, " ", item.GetUIDisplayName());
                }
            }
            if (item == null || notSet)
            {
                str3 = string.Concat(str3, " not-set");
            }
            writer.Write("<div style=\"background-image:url('{0}');background-position: left center;\" class='{1}'>", HttpUtility.HtmlEncode(themedImageSource), str3);
            writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent(str), Translate.Text("Select"));
            writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent(str1), Translate.Text("Reset"));
            writer.Write("<span title=\"{0}\">{1}</span>", (item == null ? string.Empty : item.GetUIDisplayName()), str2);
            writer.Write("</div>");
        }

        private void RenderPicker(HtmlTextWriter writer, string datasource, string clickCommand, string resetCommand, bool prependEllipsis, bool notSet = false)
        {
            Assert.ArgumentNotNull(writer, "writer");
            Assert.ArgumentNotNull(clickCommand, "clickCommand");
            Assert.ArgumentNotNull(resetCommand, "resetCommand");
            string str = string.Concat(clickCommand, "(\\\"{ID}\\\")");
            string str1 = string.Concat(resetCommand, "(\\\"{ID}\\\")");
            string str2 = Translate.Text("[Not set]");
            string str3 = "item-picker";
            if (!datasource.IsNullOrEmpty())
            {
                str2 = (!notSet ? datasource : string.Concat(str2, " ", datasource));
            }
            if (datasource.IsNullOrEmpty() || notSet)
            {
                str3 = string.Concat(str3, " not-set");
            }
            writer.Write(string.Format("<div class='{0}'>", str3));
            writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent(str), Translate.Text("Select"));
            writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent(str1), Translate.Text("Reset"));
            string str4 = str2;
            if (str4 != null && str4.Length > 15)
            {
                str4 = string.Concat(str4.Substring(0, 14), "...");
            }
            writer.Write("<span title=\"{0}\">{1}</span>", str2, str4);
            writer.Write("</div>");
        }

        private void RenderRuleActions(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(writer, "writer");
            bool flag = this.IsComponentDisplayed(rule);
            writer.Write("<div id='{ID}_hiderendering' class='hide-rendering'>");
            this.RenderHideRenderingAction(writer, Translate.Text("Show"), flag, 0, null);
            this.RenderHideRenderingAction(writer, Translate.Text("Hide"), !flag, 1, "margin-left:35px;");
            writer.Write("</div>");
            string str = (flag ? string.Empty : " display-off");
            string str1 = (this.ComponentPersonalization.Checked ? string.Empty : " style='display:none'");
            string[] strArrays = new string[] { "<div id='{ID}_setrendering' class='set-rendering", str, "'", str1, ">" };
            writer.Write(string.Concat(strArrays));
            this.RenderSetRenderingAction(rule, writer);
            writer.Write("</div>");
            writer.Write(string.Concat("<div id='{ID}_setdatasource' class='set-datasource", str, "'>"));
            this.RenderSetDatasourceAction(rule, writer);
            writer.Write("</div>");
        }

        private void RenderRuleConditions(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(writer, "writer");
            bool flag = PersonalizationFormWithActions.IsDefaultCondition(rule);
            if (!flag)
            {
                Button button = new Button()
                {
                    Header = Translate.Text("Edit rule"),
                    ToolTip = Translate.Text("Edit this rule"),
                    Class = "scButton edit-button",
                    Click = "EditConditionClick(\\\"{ID}\\\")"
                };
                writer.Write(HtmlUtil.RenderControl(button));
            }
            writer.Write(string.Concat("<div id='{ID}_rule' class='", (!flag ? "condition-container" : "condition-container default"), "'>"));
            writer.Write((flag ? this.ConditionDescriptionDefault : PersonalizationFormWithActions.GetRuleConditionsHtml(rule)));
            writer.Write("</div>");
        }

        private string RenderRulePlaceholder(string placeholderName, XElement rule)
        {
            if (this.ContextItem == null)
            {
                return string.Empty;
            }
            ItemUri uri = this.ContextItem.Uri;
            ID d = ID.Parse(this.DeviceId);
            ID d1 = ID.Parse(this.RenderingDefition.UniqueId);
            return RenderRulePlaceholderPipeline.Run(placeholderName, uri, d, d1, rule);
        }

        private void RenderRules()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<div id='non-default-container'>");
            foreach (XElement xElement in this.RulesSet.Elements("rule"))
            {
                if (PersonalizationFormWithActions.IsDefaultCondition(xElement))
                {
                    stringBuilder.Append("</div>");
                }
                stringBuilder.Append(this.GetRuleSectionHtml(xElement));
            }
            this.RulesContainer.InnerHtml = stringBuilder.ToString();
        }

        private void RenderSetDatasourceAction(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(writer, "writer");
            string datasource = this.RenderingDefition.Datasource;
            XElement actionById = PersonalizationFormWithActions.GetActionById(rule, this.SetDatasourceActionId);
            bool flag = true;
            if (actionById == null)
            {
                datasource = string.Empty;
            }
            else
            {
                datasource = actionById.GetAttributeValue("DataSource");
                flag = false;
            }
            Item contextItem = null;
            bool flag1 = false;
            if (string.IsNullOrEmpty(datasource))
            {
                contextItem = this.ContextItem;
                flag1 = true;
            }
            else
            {
                contextItem = Client.ContentDatabase.GetItem(datasource);
            }
            writer.Write(string.Concat("<div ", (!flag ? string.Empty : "class='default-values'"), ">"));
            writer.Write("<span class='section-header' unselectable='on'>");
            writer.Write(Translate.Text("Content:"));
            writer.Write("</span>");
            if (contextItem != null)
            {
                this.RenderPicker(writer, contextItem, "SetDatasourceClick", "ResetDatasource", !flag1, flag1);
            }
            else
            {
                this.RenderPicker(writer, datasource, "SetDatasourceClick", "ResetDatasource", !flag1, flag1);
            }
            writer.Write("</div>");
        }

        private void RenderSetRenderingAction(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(writer, "writer");
            string itemID = this.RenderingDefition.ItemID;
            XElement actionById = PersonalizationFormWithActions.GetActionById(rule, this.SetRenderingActionId);
            bool flag = true;
            if (actionById != null)
            {
                string attributeValue = actionById.GetAttributeValue("RenderingItem");
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    itemID = attributeValue;
                    flag = false;
                }
            }
            writer.Write(string.Concat("<div ", (!flag ? string.Empty : "class='default-values'"), ">"));
            if (string.IsNullOrEmpty(itemID))
            {
                writer.Write("</div>");
                return;
            }
            Item item = Client.ContentDatabase.GetItem(itemID);
            if (item == null)
            {
                writer.Write("</div>");
                return;
            }
            writer.Write("<span class='section-header' unselectable='on'>");
            writer.Write(Translate.Text("Presentation:"));
            writer.Write("</span>");
            string themedImageSource = Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id48x48);
            if (!string.IsNullOrEmpty(item.Appearance.Thumbnail) && item.Appearance.Thumbnail != Settings.DefaultThumbnail)
            {
                string thumbnailSrc = UIUtil.GetThumbnailSrc(item, 128, 128);
                if (!string.IsNullOrEmpty(thumbnailSrc))
                {
                    themedImageSource = thumbnailSrc;
                }
            }
            writer.Write("<div style=\"background-image:url('{0}')\" class='thumbnail-container'>", HttpUtility.HtmlEncode(themedImageSource));
            writer.Write("</div>");
            writer.Write("<div class='picker-container'>");
            this.RenderPicker(writer, item, "SetRenderingClick", "ResetRendering", false, false);
            writer.Write("</div>");
            writer.Write("</div>");
        }

        protected void ResetDatasource(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            if (!this.IsComponentDisplayed(id))
            {
                return;
            }
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, id);
            if (ruleById != null)
            {
                PersonalizationFormWithActions.RemoveAction(ruleById, this.SetDatasourceActionId);
                this.RulesSet = rulesSet;
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                this.RenderSetDatasourceAction(ruleById, htmlTextWriter);
                SheerResponse.SetInnerHtml(string.Concat(id, "_setdatasource"), htmlTextWriter.InnerWriter.ToString().Replace("{ID}", id));
            }
        }

        protected void ResetRendering(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            if (!this.IsComponentDisplayed(id))
            {
                return;
            }
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, id);
            if (ruleById != null)
            {
                PersonalizationFormWithActions.RemoveAction(ruleById, this.SetRenderingActionId);
                this.RulesSet = rulesSet;
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                this.RenderSetRenderingAction(ruleById, htmlTextWriter);
                SheerResponse.SetInnerHtml(string.Concat(id, "_setrendering"), htmlTextWriter.InnerWriter.ToString().Replace("{ID}", id));
            }
        }

        protected void SetDatasource(ClientPipelineArgs args)
        {
            Language language;
            Assert.ArgumentNotNull(args, "args");
            string item = args.Parameters["id"];
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, item);
            Assert.IsNotNull(ruleById, "rule");
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    (PersonalizationFormWithActions.GetActionById(ruleById, this.SetDatasourceActionId) ?? PersonalizationFormWithActions.AddAction(ruleById, this.SetDatasourceActionId)).SetAttributeValue("DataSource", args.Result);
                    this.RulesSet = rulesSet;
                    HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                    this.RenderSetDatasourceAction(ruleById, htmlTextWriter);
                    SheerResponse.SetInnerHtml(string.Concat(item, "_setdatasource"), htmlTextWriter.InnerWriter.ToString().Replace("{ID}", item));
                }
                return;
            }
            XElement actionById = PersonalizationFormWithActions.GetActionById(ruleById, this.SetRenderingActionId);
            Item item1 = null;
            if (actionById != null && !string.IsNullOrEmpty(actionById.GetAttributeValue("RenderingItem")))
            {
                item1 = Client.ContentDatabase.GetItem(actionById.GetAttributeValue("RenderingItem"));
            }
            else if (!string.IsNullOrEmpty(this.RenderingDefition.ItemID))
            {
                item1 = Client.ContentDatabase.GetItem(this.RenderingDefition.ItemID);
            }
            if (item1 == null)
            {
                SheerResponse.Alert("Item not found.", new string[0]);
                return;
            }
            Item contextItem = this.ContextItem;
            GetRenderingDatasourceArgs getRenderingDatasourceArg = new GetRenderingDatasourceArgs(item1)
            {
                FallbackDatasourceRoots = new List<Item>()
                {
                    Client.ContentDatabase.GetRootItem()
                }
            };
            GetRenderingDatasourceArgs getRenderingDatasourceArg1 = getRenderingDatasourceArg;
            if (contextItem != null)
            {
                language = contextItem.Language;
            }
            else
            {
                language = null;
            }
            getRenderingDatasourceArg1.ContentLanguage = language;
            getRenderingDatasourceArg.ContextItemPath = (contextItem != null ? contextItem.Paths.FullPath : string.Empty);
            getRenderingDatasourceArg.ShowDialogIfDatasourceSetOnRenderingItem = true;
            GetRenderingDatasourceArgs datasource = getRenderingDatasourceArg;
            XElement xElement = PersonalizationFormWithActions.GetActionById(ruleById, this.SetDatasourceActionId);
            if (xElement == null || string.IsNullOrEmpty(xElement.GetAttributeValue("DataSource")))
            {
                datasource.CurrentDatasource = this.RenderingDefition.Datasource;
            }
            else
            {
                datasource.CurrentDatasource = xElement.GetAttributeValue("DataSource");
            }
            if (string.IsNullOrEmpty(datasource.CurrentDatasource))
            {
                datasource.CurrentDatasource = contextItem.ID.ToString();
            }
            CorePipeline.Run("getRenderingDatasource", datasource);
            if (string.IsNullOrEmpty(datasource.DialogUrl))
            {
                SheerResponse.Alert("An error occurred.", new string[0]);
                return;
            }
            SheerResponse.ShowModalDialog(datasource.DialogUrl, "960px", "660px", string.Empty, true);
            args.WaitForPostBack();
        }

        protected void SetDatasourceClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            if (!this.IsComponentDisplayed(id))
            {
                return;
            }
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection["id"] = id;
            Context.ClientPage.Start(this, "SetDatasource", nameValueCollection);
        }

        protected void SetRendering(ClientPipelineArgs args)
        {
            string result;
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                string placeholder = this.RenderingDefition.Placeholder;
                Assert.IsNotNull(placeholder, "placeholder");
                string layout = this.Layout;
                GetPlaceholderRenderingsArgs getPlaceholderRenderingsArg = new GetPlaceholderRenderingsArgs(placeholder, layout, Client.ContentDatabase, ID.Parse(this.DeviceId))
                {
                    OmitNonEditableRenderings = true
                };
                getPlaceholderRenderingsArg.Options.ShowOpenProperties = false;
                CorePipeline.Run("getPlaceholderRenderings", getPlaceholderRenderingsArg);
                string dialogURL = getPlaceholderRenderingsArg.DialogURL;
                if (string.IsNullOrEmpty(dialogURL))
                {
                    SheerResponse.Alert("An error occurred.", new string[0]);
                    return;
                }
                SheerResponse.ShowModalDialog(dialogURL, "720px", "470px", string.Empty, true);
                args.WaitForPostBack();
                return;
            }
            if (args.HasResult)
            {
                if (args.Result.IndexOf(',') < 0)
                {
                    result = args.Result;
                }
                else
                {
                    string str = args.Result;
                    char[] chrArray = new char[] { ',' };
                    result = str.Split(chrArray)[0];
                }
                XElement rulesSet = this.RulesSet;
                string item = args.Parameters["id"];
                XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, item);
                Assert.IsNotNull(ruleById, "rule");
                (PersonalizationFormWithActions.GetActionById(ruleById, this.SetRenderingActionId) ?? PersonalizationFormWithActions.AddAction(ruleById, this.SetRenderingActionId)).SetAttributeValue("RenderingItem", ShortID.DecodeID(result));
                this.RulesSet = rulesSet;
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                this.RenderSetRenderingAction(ruleById, htmlTextWriter);
                SheerResponse.SetInnerHtml(string.Concat(item, "_setrendering"), htmlTextWriter.InnerWriter.ToString().Replace("{ID}", item));
            }
        }

        protected void SetRenderingClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            if (!this.IsComponentDisplayed(id))
            {
                return;
            }
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection["id"] = id;
            Context.ClientPage.Start(this, "SetRendering", nameValueCollection);
        }

        protected void ShowConfirm(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                SheerResponse.Confirm("Personalize component settings will be removed. Are you sure you want to continue?");
                args.WaitForPostBack();
                return;
            }
            if (!args.HasResult || !(args.Result != "no"))
            {
                this.ComponentPersonalization.Checked = true;
                return;
            }
            SheerResponse.Eval("scTogglePersonalizeComponentSection()");
            XElement rulesSet = this.RulesSet;
            foreach (XElement xElement in rulesSet.Elements("rule"))
            {
                XElement actionById = PersonalizationFormWithActions.GetActionById(xElement, this.SetRenderingActionId);
                if (actionById == null)
                {
                    continue;
                }
                actionById.Remove();
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                this.RenderSetRenderingAction(xElement, htmlTextWriter);
                ShortID shortID = ShortID.Parse(xElement.GetAttributeValue("uid"));
                Assert.IsNotNull(shortID, "ruleId");
                SheerResponse.SetInnerHtml(string.Concat(shortID, "_setrendering"), htmlTextWriter.InnerWriter.ToString().Replace("{ID}", shortID.ToString()));
            }
            this.RulesSet = rulesSet;
        }

        protected void SwitchRenderingClick(string id)
        {
            Assert.ArgumentNotNull(id, "id");
            XElement rulesSet = this.RulesSet;
            XElement ruleById = PersonalizationFormWithActions.GetRuleById(rulesSet, id);
            if (ruleById != null)
            {
                if (this.IsComponentDisplayed(ruleById))
                {
                    PersonalizationFormWithActions.AddAction(ruleById, this.HideRenderingActionId);
                }
                else
                {
                    PersonalizationFormWithActions.RemoveAction(ruleById, this.HideRenderingActionId);
                }
                this.RulesSet = rulesSet;
            }
        }
    }
}