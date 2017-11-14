/*================================================================================================================================

  This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.  

  THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, 
  INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.  

  We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object 
  code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to market Your software 
  product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your software product in which the 
  Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims 
  or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.

 =================================================================================================================================*/

using LINQPad.Extensibility.DataContext;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.OData.Client;
using Microsoft.Pfe.Xrm.View;
using System;
using System.Collections.Generic;
//using System.Data.Services.Client;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Microsoft.Pfe.Xrm
{
    /// <summary>
    /// Create StaticDataContextDriver, though it is dynamically generated. By using Static, it is easy to reuse generated assembly.
    /// </summary>
    public class CRMLinqPadDriverWebAPI : StaticDataContextDriver
    {
        // Author
        public override string Author
        {
            get { return "Kenichiro Nakamura"; }
        }

        // Display Name for connection.
        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            CrmProperties props = new CrmProperties(cxInfo);
            return props.FriendlyName + "(Web API) " + props.OrgUri;
        }

        // Display Name for Driver.
        public override string Name
        {
            get { return "Dynamics 365 CE Web API Linq Pad Driver"; }
        }

        /// <summary>
        /// Opens Login dialog
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <param name="isNewConnection">Indicate if this is new connection request or update existing connection</param>
        /// <returns>result</returns>
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
        {
            return new MainWindow(cxInfo, isNewConnection).ShowDialog() == true;
        }

        /// <summary>
        /// Additional work for Initialization. Hook PreExecute event to display QueryExpression and FetchXML to SQL tab.
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <param name="context">Context generated via CSDL</param>
        /// <param name="executionManager">QueryExecutionManager</param>
        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            CrmProperties props = new CrmProperties(cxInfo);

            // Set AccessToken when calling REST Endpoint.
            (context as Microsoft.OData.Client.DataServiceContext).SendingRequest2 += (s, e) =>
            {
                switch (props.AuthenticationProviderType)
                {
                    // If this is for CRM Online, then use OAuth 2.0
                    case "OnlineFederation":
                    case "Federation":
                        AuthenticationContext authContext = new AuthenticationContext(props.Authority, false);
                        string accessToken;
                        try
                        {
                            // Try to get token without prompt fist.
                            accessToken = authContext.AcquireTokenSilent(props.OrgUri, props.ClientId).AccessToken;
                        }
                        catch
                        {
                            // If AcquireTokenSilent fails, then use another way 
                            if (props.AuthenticationProviderType == "OnlineFederation")
                            {
                                try
                                {
                                    // Try to use embed credential
                                    accessToken = authContext.AcquireToken(props.OrgUri, props.ClientId, new UserCredential(props.UserName, props.Password)).AccessToken;
                                }
                                catch (Exception ex)
                                {
                                    // If failed, need to try using consent.
                                    accessToken = authContext.AcquireToken(props.OrgUri, props.ClientId, new Uri(props.RedirectUri), PromptBehavior.Always, new UserIdentifier(props.UserName, UserIdentifierType.RequiredDisplayableId)).AccessToken;
                                }
                            }
                            else
                                accessToken = authContext.AcquireToken(props.OrgUri, props.ClientId, new Uri(props.RedirectUri), PromptBehavior.Always).AccessToken;
                        }
                        e.RequestMessage.SetHeader("Authorization", "Bearer " + accessToken);
                        break;
                    // If this is ActiveDirectory, then use Windows Integrated Authentication.
                    // Web API is only supported Online at the moment, so comment out the following for future use.
                    case "ActiveDirectory":
                        if (String.IsNullOrEmpty(props.DomainName))
                            (e.RequestMessage as HttpWebRequestMessage).Credentials = CredentialCache.DefaultCredentials;
                        else
                            (e.RequestMessage as HttpWebRequestMessage).Credentials = new NetworkCredential(props.UserName, props.Password, props.DomainName);
                        break;
                }
                LINQPad.Util.ClearResults();
                executionManager.SqlTranslationWriter.WriteLine(e.RequestMessage.Url);
            };

            base.InitializeContext(cxInfo, context, executionManager);
        }

        /// <summary>
        /// Pass Extended OrganizationService to context
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <returns>Constructor argument(s)</returns>
        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            // Instantiate CrmProperties.
            CrmProperties props = new CrmProperties(cxInfo);

            return new object[]
            {
                new Uri(props.OrgUri + "/api/data/v" + props.Version + "/")
            };
        }

        /// <summary>
        /// Specify Context Constructor argument type(s)
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <returns>Constructor argument type(s)</returns>
        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            return new ParameterDescriptor[]
            {
                // OrgainzationService is the only constructor argument.
                new ParameterDescriptor("serviceRoot", "System.Uri")
            };
        }

        /// <summary>
        /// Load additional assemblies to LinqPad process.
        /// </summary>
        /// <returns>List of assmeblies to be loaded.</returns>
        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
            {
                "System.ServiceModel.dll",
                "Microsoft.Data.Edm.dll",
                "Microsoft.Data.OData.dll",
                "Microsoft.Data.Services.Client.dll",
                "System.Runtime.Serialization.dll",
                "Microsoft.OData.Client.dll",
                "Microsoft.OData.Core.dll",
                "Microsoft.OData.Edm.dll",
                "Microsoft.Spatial.dll",
                "System.Spatial.dll"
            };
        }

        /// <summary>
        /// Import additional namespaces.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
            {
                "Microsoft.OData.Client",
                "System.ServiceModel",
                "System.ServiceModel.Description"
            };
        }

        /// <summary>
        /// Generate Schema information.
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <param name="customType">Context Type</param>
        /// <returns>Schema Information.</returns>
        public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType)
        {
            // Instantiate CrmProperties.
            CrmProperties props = new CrmProperties(cxInfo);
            // Instantiate ExplorerItem list.
            List<ExplorerItem> schema = new List<ExplorerItem>();

            // Create Tables for Schema.
            foreach (PropertyInfo prop in customType.GetRuntimeProperties())
            {
                // If property is not Generic Type or IQueryable, then ignore.
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.Name != "DataServiceQuery`1")
                    continue;

                // Create ExploreItem with Table icon as this is top level item.
                ExplorerItem item = new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    // Store Entity Type to Tag.
                    Tag = prop.PropertyType.GenericTypeArguments[0].Name,
                    IsEnumerable = true,
                    Children = new List<ExplorerItem>()
                };

                schema.Add(item);
            }

            schema = schema.OrderBy(x => x.Text).ToList<ExplorerItem>();

            // Then create columns for each table. Loop through tables again.
            foreach (PropertyInfo prop in customType.GetRuntimeProperties())
            {
                // Obtain Table item from table lists.
                var item = schema.Where(x => x.Text == prop.Name).FirstOrDefault();

                if (item == null)
                    continue;

                // Get all property from Entity for the table. (i.e. Account for AccountSet)
                foreach (PropertyInfo childprop in customType.Module.GetTypes().Where(x => x.Name == prop.PropertyType.GenericTypeArguments[0].Name).First().GetRuntimeProperties())
                {
                    // If property is IEnumerable type, then it is 1:N or N:N relationship field.
                    // Need to find a way to figure out if this is 1:N or N:N. At the moment, I just make them as OneToMany type.
                    if (childprop.PropertyType.IsGenericType && childprop.PropertyType.Name == "IEnumerable`1")
                    {
                        // Try to get LinkTarget. 
                        ExplorerItem linkTarget = schema.Where(x => x.Tag.ToString() == childprop.PropertyType.GetGenericArguments()[0].Name).FirstOrDefault();
                        if (linkTarget == null)
                            continue;

                        // Create ExplorerItem as Colleciton Link.
                        item.Children.Add(
                            new ExplorerItem(
                                childprop.Name,
                                ExplorerItemKind.CollectionLink,
                                ExplorerIcon.OneToMany)
                            {
                                HyperlinkTarget = linkTarget,
                                ToolTipText = DataContextDriver.FormatTypeName(childprop.PropertyType, false)
                            });
                    }
                    else
                    {
                        // Try to get LinkTarget to check if this field is N:1.
                        ExplorerItem linkTarget = schema.Where(x => x.Tag.ToString() == childprop.PropertyType.Name).FirstOrDefault();

                        // If no linkTarget exists, then this is normal field.
                        if (linkTarget == null)
                        {
                            // Create ExplorerItem as Column.
                            item.Children.Add(
                                new ExplorerItem(
                                    childprop.Name + " (" + DataContextDriver.FormatTypeName(childprop.PropertyType, false) + ")",
                                    ExplorerItemKind.Property,
                                    ExplorerIcon.Column)
                                {
                                    ToolTipText = DataContextDriver.FormatTypeName(childprop.PropertyType, false)
                                });
                        }
                        else
                        {
                            // Otherwise, create ExploreItem as N:1
                            item.Children.Add(
                                new ExplorerItem(
                                    childprop.Name + " (" + DataContextDriver.FormatTypeName(childprop.PropertyType, false) + ")",
                                    ExplorerItemKind.ReferenceLink,
                                    ExplorerIcon.ManyToOne)
                                {
                                    HyperlinkTarget = linkTarget,
                                    ToolTipText = DataContextDriver.FormatTypeName(childprop.PropertyType, false)
                                });
                        }
                    }

                    // Order Fields
                    item.Children = item.Children.OrderBy(x => x.Text).ToList<ExplorerItem>();
                }
            }

            // Order Entities.
            schema = schema.OrderBy(x => x.Text).ToList<ExplorerItem>();
            
            return schema;
        }
    }
}
