using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ServiceStack.Host;
using ServiceStack.Support.WebHost;
using ServiceStack.Templates;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class IndexOperationsControl : System.Web.UI.Control
    {
        public IRequest Request { get; set; }
        public string Title { get; set; }
        public List<string> OperationNames { get; set; }
        public IDictionary<int, string> Xsds { get; set; }
        public int XsdServiceTypesIndex { get; set; }
        public MetadataPagesConfig MetadataConfig { get; set; }

        public string RenderRow(string operationName)
        {
            var show = HostContext.DebugMode //Show in DebugMode
                && !MetadataConfig.AlwaysHideInMetadata(operationName); //Hide When [Restrict(VisibilityTo = None)]

            // use a fully qualified path if WebHostUrl is set
            string baseUrl = Request.ResolveAbsoluteUrl("~/");

            var opType = HostContext.Metadata.GetOperationType(operationName);
            var op = HostContext.Metadata.GetOperation(opType);

            var icons = CreateIcons(op);

            var opTemplate = new StringBuilder("<tr><th>" + icons + "{0}</th>");
            foreach (var config in MetadataConfig.AvailableFormatConfigs)
            {
                var uri = baseUrl.CombineWith(config.DefaultMetadataUri);
                if (MetadataConfig.IsVisible(Request, config.Format.ToFormat(), operationName))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">{1}</a></td>", uri, config.Name);
                }
                else
                {
                    opTemplate.AppendFormat("<td>{0}</td>", config.Name);
                }
            }

            opTemplate.Append("</tr>");

            return show ? string.Format(opTemplate.ToString(), operationName) : "";
        }

        private static string CreateIcons(Operation op)
        {
            var sbIcons = new StringBuilder();
            if (op.RequiresAuthentication)
            {
                sbIcons.Append("<i class=\"auth\" title=\"");

                var hasRoles = op.RequiredRoles.Count + op.RequiresAnyRole.Count > 0;
                if (hasRoles)
                {
                    sbIcons.Append("Requires Roles:");
                    var sbRoles = new StringBuilder();
                    foreach (var role in op.RequiredRoles)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(",");

                        sbRoles.Append(" " + role);
                    }

                    foreach (var role in op.RequiresAnyRole)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(", ");

                        sbRoles.Append(" " + role + "?");
                    }
                    sbIcons.Append(sbRoles);
                }

                var hasPermissions = op.RequiredPermissions.Count + op.RequiresAnyPermission.Count > 0;
                if (hasPermissions)
                {
                    if (hasRoles)
                        sbIcons.Append(". ");

                    sbIcons.Append("Requires Permissions:");
                    var sbPermission = new StringBuilder();
                    foreach (var permission in op.RequiredPermissions)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission);
                    }

                    foreach (var permission in op.RequiresAnyPermission)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission + "?");
                    }
                    sbIcons.Append(sbPermission);
                }

                if (!hasRoles && !hasPermissions)
                    sbIcons.Append("Requires Authentication");

                sbIcons.Append("\"></i>");
            }

            var icons = sbIcons.Length > 0
                ? "<span class=\"icons\">" + sbIcons + "</span>"
                : "";
            return icons;
        }

        protected override void Render(HtmlTextWriter output)
        {
            var operationsPart = new TableTemplate
            {
                Title = "Operations",
                Items = this.OperationNames,
                ForEachItem = RenderRow
            }.ToString();

            var xsdsPart = new ListTemplate
            {
                Title = "XSDS:",
                ListItemsIntMap = this.Xsds,
                ListItemTemplate = @"<li><a href=""?xsd={0}"">{1}</a></li>"
            }.ToString();

            var wsdlTemplate = new StringBuilder();
            var soap11Config = MetadataConfig.GetMetadataConfig("soap11") as SoapMetadataConfig;
            var soap12Config = MetadataConfig.GetMetadataConfig("soap12") as SoapMetadataConfig;
            if (soap11Config != null || soap12Config != null)
            {
                wsdlTemplate.AppendLine("<h3>WSDLS:</h3>");
                wsdlTemplate.AppendLine("<ul>");
                if (soap11Config != null)
                {
                    wsdlTemplate.AppendFormat(
                        @"<li><a href=""{0}"">{0}</a></li>",
                        soap11Config.WsdlMetadataUri);
                }
                if (soap12Config != null)
                {
                    wsdlTemplate.AppendFormat(
                        @"<li><a href=""{0}"">{0}</a></li>",
                        soap12Config.WsdlMetadataUri);
                }
                wsdlTemplate.AppendLine("</ul>");
            }

            var metadata = HostContext.GetPlugin<MetadataFeature>();
            var pluginLinks = metadata != null && metadata.PluginLinks.Count > 0
                ? new ListTemplate
                {
                    Title = metadata.PluginLinksTitle,
                    ListItemsMap = metadata.PluginLinks,
                    ListItemTemplate = @"<li><a href=""{0}"">{1}</a></li>"
                }.ToString()
                : "";

            var debugOnlyInfo = HostContext.DebugMode && metadata != null && metadata.DebugLinks.Count > 0
                ? new ListTemplate
                {
                    Title = metadata.DebugLinksTitle,
                    ListItemsMap = metadata.DebugLinks,
                    ListItemTemplate = @"<li><a href=""{0}"">{1}</a></li>"
                }.ToString()
                : "";

            var renderedTemplate = HtmlTemplates.Format(
                HtmlTemplates.GetIndexOperationsTemplate(),
                this.Title,
                this.XsdServiceTypesIndex,
                operationsPart,
                xsdsPart,
                wsdlTemplate,
                pluginLinks,
                debugOnlyInfo);

            output.Write(renderedTemplate);
        }

    }
}