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
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.CSharp;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Pfe.Xrm.Common;
using Microsoft.Pfe.Xrm.View;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk.Discovery;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Pfe.Xrm.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Property

        #region Visibility

        private bool isLoginVisible;
        public bool IsLoginVisible
        {
            get { return isLoginVisible; }
            set
            {
                isLoginVisible = value;
                NotifyPropertyChanged();
            }
        }

        private bool isAutoRegister;
        public bool IsAutoRegister
        {
            get { return isAutoRegister; }
            set
            {
                isAutoRegister = value;
                NotifyPropertyChanged();
            }
        }

        #endregion
        
        private string registerText;
        public string RegisterText
        {
            get { return registerText; }
            set
            {
                registerText = value;
                NotifyPropertyChanged();
            }
        }

        private string loadMessage;
        public string LoadMessage
        {
            get { return loadMessage; }
            set
            {
                loadMessage = value;
                NotifyPropertyChanged();
            }
        }
        
        private string message;
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                NotifyPropertyChanged();
            }
        }

        public string ClientId
        {
            get { return props.ClientId; }
            set
            {
                props.ClientId = value;
                NotifyPropertyChanged();
            }
        }

        public string RedirectUri
        {
            get { return props.RedirectUri; }
            set
            {
                props.RedirectUri = value;
                NotifyPropertyChanged();
            }
        }

        private bool isNewConnection;
        public bool IsNewConnection
        {
            get { return isNewConnection; }
            set
            {
                isNewConnection = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsLoaded;
        private CrmProperties props;
        private string contextName;
        private bool useCurrentUser;
        private string metadataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "$metadata.xml");
        private IConnectionInfo cxInfo;

        #endregion

        #region Method

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowViewModel(IConnectionInfo cxInfo, bool isNewConnection)
        {
            this.cxInfo = cxInfo;
            IsNewConnection = isNewConnection;
            // Display message depending on isNewConnection
            Message = IsNewConnection ? "Click Login to CRM." : "Click to download latest metadata";

            IsAutoRegister = true;
            useCurrentUser = true;

            // Change button visibility depending on if this is New Connection or not.
            if (IsNewConnection)
                IsLoginVisible = true;

            props = new CrmProperties(cxInfo);
        }

        /// <summary>
        /// Launch CrmLogin and let user login, then set CrmProperties.
        /// </summary>
        private async Task<bool> LoginToCrm()
        {
            bool isSuccess = false;

            // Establish the Login control
            CrmLogin ctrl = new CrmLogin();
            // Wire Event to login response. 
            ctrl.ConnectionToCrmCompleted += ctrl_ConnectionToCrmCompleted;
            // Show the dialog. 
            ctrl.ShowDialog();

            // Let UI go.
            await Task.Delay(1);
            
            if (ctrl.CrmConnectionMgr != null && ctrl.CrmConnectionMgr.CrmSvc != null && ctrl.CrmConnectionMgr.CrmSvc.IsReady)
            {
                IsLoading = true;

                LoadMessage = "Signing to Dynamics CRM....";

                // Assign local property
                props.OrgUri = ctrl.CrmConnectionMgr.ConnectedOrgPublishedEndpoints[EndpointType.WebApplication];
                props.FriendlyName = ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgFriendlyName;
                props.AuthenticationProviderType = ctrl.CrmConnectionMgr.CrmSvc.OrganizationServiceProxy.ServiceConfiguration.AuthenticationType.ToString();

                if (props.AuthenticationProviderType == "OnlineFederation")
                    props.Authority = await DiscoveryAuthority();
                else if (props.AuthenticationProviderType == "Federation")
                    props.Authority = ctrl.CrmConnectionMgr.CrmSvc.OrganizationServiceProxy.ServiceConfiguration.CurrentIssuer.IssuerAddress.Uri.AbsoluteUri.Replace(ctrl.CrmConnectionMgr.CrmSvc.OrganizationServiceProxy.ServiceConfiguration.CurrentIssuer.IssuerAddress.Uri.AbsolutePath, "/adfs/ls");
                
                // Store User Credentials.
                ClientCredentials credentials = ctrl.CrmConnectionMgr.CrmSvc.OrganizationServiceProxy.ClientCredentials;
                if (credentials.UserName.UserName != null)
                {
                    props.UserName = credentials.UserName.UserName;
                    props.Password = credentials.UserName.Password;
                }
                else if (credentials.Windows.ClientCredential.UserName != null)
                {
                    props.DomainName = credentials.Windows.ClientCredential.Domain;
                    props.UserName = credentials.Windows.ClientCredential.UserName;
                    props.Password = credentials.Windows.ClientCredential.Password;
                }

                // Version
                if (ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgVersion != null)
                    props.Version = ctrl.CrmConnectionMgr.CrmSvc.ConnectedOrgVersion.ToString();
                else
                    props.Version = await CheckVersion();
                
                if(Version.Parse(props.Version).Major < 8)
                    MessageBox.Show("WebAPI is available after Dynamics CRM 2016 only.");
                else
                    isSuccess = true;

                IsLoading = false;
            }
            else
                MessageBox.Show("BadConnect");

            return isSuccess;
        }

        /// <summary>
        /// Raised when the login form process is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ctrl_ConnectionToCrmCompleted(object sender, EventArgs e)
        {
            if (sender is CrmLogin)
            {
                ((CrmLogin)sender).Close();
            }
        }

        /// <summary>
        /// Discover the authority for authentication.
        /// </summary>
        /// <param name="serviceUrl">The SOAP endpoint for a tenant organization.</param>
        /// <returns>The decoded authority URL.</returns>
        /// <remarks>The passed service URL string must contain the SdkClientVersion property.
        /// Otherwise, the discovery feature will not be available.</remarks>
        public async Task<string> DiscoveryAuthority()
        {
            AuthenticationParameters ap = await
                AuthenticationParameters.CreateFromResourceUrlAsync(new Uri(props.OrgUri + "/api/data/"));
            return ap.Authority;
        }

        /// <summary>
        /// Get Dynamics CRM Organization version 
        /// </summary>
        /// <returns>Version Number</returns>
        public async Task<string> CheckVersion()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Get Organization Name
                    var orgName = props.OrgUri.Substring(8, props.OrgUri.IndexOf(".") - 8);
                    // Get version by calling discovery
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AcquireToken());
                    HttpResponseMessage httpResponse = await httpClient.GetAsync(props.OrgUri.Replace(orgName, "disco") + "/api/discovery/v8.0/Instances");

                    // Get results, check Organization Name
                    var results = Newtonsoft.Json.Linq.JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result)["value"];
                    var org = results.Where(x => x["UrlName"].ToString() == orgName).FirstOrDefault();

                    return org["Version"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Couldn't retrieve version");
            }
        }

        /// <summary>
        /// Get OAuth 2.0 AccessToken by using ADAL.
        /// </summary>
        /// <returns>AccessToken</returns>
        private string AcquireToken()
        {
            AuthenticationResult result = null;
            int i = 0;

            AuthenticationContext authContext = new AuthenticationContext(props.Authority, false);
            authContext.TokenCache.Clear();
            var credential = new UserCredential(props.UserName, props.Password);

            // Try to get AuthenticationResult in a loop. If application is just registered to Azure AD,
            // it sometime fails to find the application and crashes.
            while (true)
            {
                try
                {
                    if (props.AuthenticationProviderType == "OnlineFederation")
                    {
                        try
                        {
                            result = authContext.AcquireToken(props.OrgUri, props.ClientId, credential);
                        }
                        catch (Exception ex)
                        {
                            // Handle only if it's consent issue.
                            if (ex.HResult == -2146233088)
                            {   
                                try
                                {
                                    result = authContext.AcquireToken(props.OrgUri, props.ClientId, new Uri(props.RedirectUri), PromptBehavior.Auto, new UserIdentifier(props.UserName, UserIdentifierType.RequiredDisplayableId));
                                }
                                catch
                                {
                                    result = authContext.AcquireToken(props.OrgUri, props.ClientId, new Uri(props.RedirectUri), PromptBehavior.Always, new UserIdentifier(props.UserName, UserIdentifierType.RequiredDisplayableId));
                                }
                            }
                        }
                    }
                    else
                        result = authContext.AcquireToken(props.OrgUri, props.ClientId, new Uri(props.RedirectUri), PromptBehavior.Auto);
                }
                catch(Exception ex)
                {
                    if (i == 2)
                    {
                        MessageBox.Show("Couldn't authenticate user. If you manually provided clientid, make sure it is registered as multi tenant application in case you access from different domain.");
                        return null;
                    }
                    i++;
                    Thread.Sleep(1000);
                }

                // Exit loop once you get AccessToken.
                if (result != null && !string.IsNullOrEmpty(result.AccessToken))
                    break;
            }

            return result.AccessToken;
        }

        /// <summary>
        /// Download Web API metadata by using OAuth 2.0 and set metadata file path.
        /// </summary>
        private async Task DownloadMetadata()
        {
            HttpClient httpClient;

            if (props.AuthenticationProviderType == "OnlineFederation" || props.AuthenticationProviderType == "Federation")
            {
                httpClient = new HttpClient(new HttpClientHandler());
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AcquireToken());
            }
            else
            {
                if (String.IsNullOrEmpty(props.DomainName))
                    httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                else
                {
                    NetworkCredential cred = new NetworkCredential(props.DomainName + "\\" + props.UserName, props.Password);
                    httpClient = new HttpClient(new HttpClientHandler() { Credentials = cred });
                }
            }

            // Download metadata from Web API endpoint.
            var response = await httpClient.GetAsync(props.OrgUri + "/api/data/v" + props.Version + "/$metadata");
            var metadata = await response.Content.ReadAsStringAsync();
            // Delete file if exist
            File.Delete(metadataFilePath);

            // Create file.
            File.WriteAllText(metadataFilePath, metadata, Encoding.UTF8);
        }

        #region Register Azure AD Application

        private AuthenticationContext authContext;
       
        /// <summary>
        /// Register Application to Azure AD
        /// </summary>
        /// <returns>registered application's clientid</returns>
        public string RegisterApp()
        {
            authContext = new AuthenticationContext(props.Authority);

            // Application Name
            string appName = "CRM for LINQPad";

            ActiveDirectoryClient activeDirectoryClient;

            int i = 0;
            while (true)
            {                
                // Instantiate ActiveDirectoryClient
                activeDirectoryClient = GetActiveDirectoryClientAsApplication(useCurrentUser);

                if (CheckAzureAdPrivilege(activeDirectoryClient))
                    break;
                else
                {
                    MessageBox.Show("Current login user does not have privilege to register an applicaiton to the Azure AD. You need to login as Company Admin so that it can reigster an applicaiton, or cancel the wizard, then enter ClientId/RedirectUri manually.");
                    // Clear the ADAL cache.
                    authContext.TokenCache.Clear();
                    useCurrentUser = false;
                    if(i==1)
                        return null;
                    else
                        i++;
                }
            }

            // Check if same name application already exists.
            var existingApp = activeDirectoryClient.Applications
                .Where(x => x.DisplayName == appName)
                .ExecuteAsync().Result.CurrentPage.FirstOrDefault();

            // If it is already registered, then return existing clientid.
            if (existingApp != null)
                return existingApp.AppId;

            // Instantiate Application to Azure AD.
            IApplication myapp = new Microsoft.Azure.ActiveDirectory.GraphClient.Application();
            myapp.DisplayName = appName;
            var redirectUri = "http://localhost/linqpad";
            myapp.ReplyUrls.Add(redirectUri);
            props.RedirectUri = redirectUri;
            myapp.PublicClient = true;
            // Mark this only to this tenant
            myapp.AvailableToOtherTenants = false;

            // Create the Application to Azure AD.
            activeDirectoryClient.Applications.AddApplicationAsync(myapp).Wait();

            // Obtain the created Application.
            var createdApp = activeDirectoryClient.Applications
                .Where(x => x.DisplayName == appName)
                .ExecuteAsync().Result.CurrentPage.FirstOrDefault();

            // Instantiate Service regarding to the application.
            IServicePrincipal myservice = new ServicePrincipal();
            myservice.AppId = createdApp.AppId;
            myservice.Tags.Add("WindowsAzureActiveDirectoryIntegratedApp");

            // Create the Service.
            activeDirectoryClient.ServicePrincipals.AddServicePrincipalAsync(myservice).Wait();

            // Obtain the created Service.
            var createdService = activeDirectoryClient.ServicePrincipals
                .Where(x => x.DisplayName == appName)
                .ExecuteAsync().Result.CurrentPage.FirstOrDefault();

            // Set permissions.
            // Get Microsoft.Azure.ActiveDirectory Service.
            var service1 = activeDirectoryClient.ServicePrincipals
                .Where(x => x.AppId == "00000002-0000-0000-c000-000000000000")
                .ExecuteAsync().Result.CurrentPage.FirstOrDefault();

            // Instantiate UserProfile.Read OAuth2PermissionGrant for the Service
            OAuth2PermissionGrant grant0 = new OAuth2PermissionGrant();
            grant0.ClientId = createdService.ObjectId;
            grant0.ResourceId = service1.ObjectId;
            grant0.ConsentType = "AllPrincipals";
            grant0.Scope = "User.Read";
            grant0.ExpiryTime = DateTime.Now.AddYears(1);

            // Create the OAuth2PermissionGrant
            activeDirectoryClient.Oauth2PermissionGrants.AddOAuth2PermissionGrantAsync(grant0).Wait();

            // Get Microsoft.CRM Service.
            var service2 = activeDirectoryClient.ServicePrincipals
                .Where(x => x.AppId == "00000007-0000-0000-c000-000000000000")
                .ExecuteAsync().Result.CurrentPage.FirstOrDefault();

            // Instantiate user_impersonation OAuth2PermissionGrant for the Service
            OAuth2PermissionGrant grant = new OAuth2PermissionGrant();
            grant.ClientId = createdService.ObjectId;
            grant.ResourceId = service2.ObjectId;
            grant.ConsentType = "AllPrincipals";
            grant.Scope = "user_impersonation";
            grant.ExpiryTime = DateTime.Now.AddYears(1);

            // Create the OAuth2PermissionGrant
            activeDirectoryClient.Oauth2PermissionGrants.AddOAuth2PermissionGrantAsync(grant).Wait();

            // Create RequiredResourceAccess
            // Instantiate ResourceAccess for Microsoft.Azure.ActiveDirectory/UserProfile.Read permission.
            ResourceAccess resourceAccess1 = new ResourceAccess();
            resourceAccess1.Id = service1.Oauth2Permissions.Where(x => x.Value == "User.Read").First().Id;
            resourceAccess1.Type = "Scope";
            // Instantiate RequiredResourceAccess and assign the ResourceAccess
            RequiredResourceAccess requiredresourceAccess1 = new RequiredResourceAccess();
            requiredresourceAccess1.ResourceAppId = service1.AppId;
            requiredresourceAccess1.ResourceAccess.Add(resourceAccess1);

            // Instantiate ResourceAccess for Microsoft.CRM/user_impersonation.Read permission.
            ResourceAccess resourceAccess2 = new ResourceAccess();
            resourceAccess2.Id = service2.Oauth2Permissions.Where(x => x.Value == "user_impersonation").First().Id;
            resourceAccess2.Type = "Scope";
            // Instantiate RequiredResourceAccess and assign the ResourceAccess
            RequiredResourceAccess requiredResourceAccess2 = new RequiredResourceAccess();
            requiredResourceAccess2.ResourceAppId = service2.AppId;
            requiredResourceAccess2.ResourceAccess.Add(resourceAccess2);

            // Add RequiredResourceAccess information to the Application
            createdApp.RequiredResourceAccess.Add(requiredresourceAccess1);
            createdApp.RequiredResourceAccess.Add(requiredResourceAccess2);

            // Update the Application
            createdApp.UpdateAsync().Wait();

            // Once all Azure AD work done, clear ADAL cache again in case user logged in as different user.
            authContext.TokenCache.Clear();

            // Return AppId (ClientId)
            return createdApp.AppId;
        }

        /// <summary>
        /// Check if login user has enough privilege for Azure AD to register application.
        /// </summary>
        /// <param name="activeDirectoryClient"></param>
        /// <returns>true if user has enough privielge, otherwise false</returns>
        private bool CheckAzureAdPrivilege(ActiveDirectoryClient activeDirectoryClient)
        {
            // Check current user's role for Azure AD.
            var roles = activeDirectoryClient.Me.MemberOf.ExecuteAsync().Result.CurrentPage;
            object role = null;

            // If user has more then (usually 1 or 0) roles, then check if its Company Administrator.
            if (roles.Count != 0)
            {
                role = roles
                .Where(x => x.GetType() == typeof(DirectoryRole))
                .Where(x => (x as DirectoryRole).DisplayName == "Company Administrator").FirstOrDefault();
            }

            // If user doesn't have enough permission, then error out.
            if (role == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Generate new instance of ActiveDirectoryClient
        /// </summary>
        /// <returns>ActiveDirectoryClient</returns>
        public ActiveDirectoryClient GetActiveDirectoryClientAsApplication(bool useCurrentUser = true)
        {
            // Create Graph Endpoint
            string servicePointUri = props.Authority.Replace("https://login.windows.net/", "https://graph.windows.net/").Replace("oauth2/authorize", "");
            Uri serviceRoot = new Uri(servicePointUri);

            // Create ActiveDirectoryClient by specifying how to get AccessToken
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,
                async () => await AcquireTokenAsyncForApplication(useCurrentUser));

            return activeDirectoryClient;
        }

        /// <summary>
        /// Acquire AccessToken for Azure AD Graph. 
        /// Client ID is obtained from Connected Service.
        /// </summary>
        /// <param name="useCurrentUser">Indicate if using currentuser</param>
        /// <returns>Access Token</returns>
        public async Task<string> AcquireTokenAsyncForApplication(bool useCurrentUser)
        {
            AuthenticationContext authContext = new AuthenticationContext(props.Authority, false);

            string token = "";

            // If using current user, then get credential from CrmProperties, otherwise popup for authentication.
            if (useCurrentUser)
                token = authContext.AcquireToken("https://graph.windows.net", "2ad88395-b77d-4561-9441-d0e40824f9bc",
                    new UserCredential(props.UserName, props.Password)).AccessToken;
            else
                token = authContext.AcquireToken("https://graph.windows.net", "2ad88395-b77d-4561-9441-d0e40824f9bc",
                   new Uri("app://5d3e90d6-aa8e-48a8-8f2c-58b45cc67315"), PromptBehavior.Always).AccessToken;

            return token;
        }

        #endregion

        /// <summary>
        /// Generate Context and Early bound class file by using CrmSvcUtil.exe.
        /// Then compile it into an assembly.
        /// </summary>
        private void LoadData()
        {
            // Instantiate T4 template object and set parameters.
            CrmODataClient odataClient = new CrmODataClient();
            odataClient.MetadataDocumentUri = metadataFilePath;
            odataClient.NamespacePrefix = "";
            odataClient.UseDataServiceCollection = true;
            odataClient.IgnoreUnexpectedElementsAndAttributes = true;

            // Store assembly full path.
            string assemblyFullName = "";
            // When reload data, we need to generate new assembly as current one is hold by LinqPad instance.
            // Switch Context.dll and ContextAlternative.dll based on what LinqPad loads right now.
            if (props._cxInfo.CustomTypeInfo.CustomAssemblyPath.EndsWith("Alternative.dll"))
                assemblyFullName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, props.OrgUri.GetHashCode() + "Context.dll");
            else
                assemblyFullName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, props.OrgUri.GetHashCode() + "ContextAlternative.dll");

            props._cxInfo.CustomTypeInfo.CustomAssemblyPath = assemblyFullName;

            // Compile the code into the assembly. To avoid duplicate name for each connection, hash entire URL to make it unique.
            BuildAssembly(odataClient.TransformText(), assemblyFullName);

            // Update message.
            Message = "Loading Complete. Click Exit and wait a while until Linq Pad displays full Schema information.";
        }

        /// <summary>
        /// Generate an assembly
        /// </summary>
        /// <param name="code">code to be compiled</param>
        /// <param name="name">assembly name</param>
        private void BuildAssembly(string code, string name)
        {
            // Use the CSharpCodeProvider to compile the generated code:
            CompilerResults results;
            using (var codeProvider = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }))
            {
                var options = new CompilerParameters(
                    "System.dll System.Core.dll System.Xml.dll System.Runtime.Serialization.dll".Split(' '),
                    name,
                    false);
                // Force load Microsoft.Xrm.Sdk assembly.
                options.ReferencedAssemblies.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.OData.Client.dll"));
                options.ReferencedAssemblies.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.OData.Core.dll"));
                options.ReferencedAssemblies.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.OData.Edm.dll"));
                options.ReferencedAssemblies.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.Spatial.dll"));

                // Compile
                results = codeProvider.CompileAssemblyFromSource(options, code);
            }
            if (results.Errors.Count > 0)
                throw new Exception
                    ("Cannot compile typed context: " + results.Errors[0].ErrorText + " (line " + results.Errors[0].Line + ")");
        }

        #endregion

        #region Command

        /// <summary>
        /// Login by using CrmLogin.
        /// </summary>
        public RelayCommand LoginCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    IsLoading = true;
                    // First of all, login to CRM.
                    if (!await LoginToCrm())
                    {
                        IsLoading = false;
                        return;
                    }

                    // Wait until all CrmProperties are set.
                    while (IsLoading)
                    {
                        await Task.Delay(10);
                    }

                    IsLoading = true;

                    // Register app to Azure AD if no ClientId provided
                    if (String.IsNullOrEmpty(props.ClientId))
                    {
                        if (props.AuthenticationProviderType == "OnlineFederation")
                        {
                            LoadMessage = "Registering Application....";
                            props.ClientId = await Task.Run(() => RegisterApp());
                        }
                        else if (props.AuthenticationProviderType == "Federation")
                        {
                            MessageBox.Show("Please provide ClientID for IFD.");
                            IsLoading = false;
                            return;
                        }
                    }

                    if (string.IsNullOrEmpty(props.ClientId))
                    {
                        IsLoading = false;
                        return;
                    }

                    // Try to get token once to make sure ClientId works
                    var token = AcquireToken();

                    if(string.IsNullOrEmpty(token))
                    {
                        IsLoading = false;
                        return;
                    }

                    // Then download metadata
                    LoadMessage = "Downloading Metadata....";
                    await DownloadMetadata();

                    // Then generate context code
                    LoadMessage = "Generating Context....";
                    await Task.Run(() => LoadData());

                    // Finally set context
                    contextName = "System";
                    // Set the assembly name and Context class name.
                    props._cxInfo.CustomTypeInfo.CustomTypeName = String.Format("Microsoft.Dynamics.CRM.{0}", contextName);

                    Message = "Loading Complete. Click Exit and wait a while until Linq Pad displays full Schema information.";
                    IsLoaded = true;
                    IsLoading = false;
                });
            }
        }

        /// <summary>
        /// Login by using CrmLogin and generate an assembly
        /// </summary>
        public RelayCommand ChangeCredentialCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    LoginToCrm();
                });
            }
        }

        /// <summary>
        /// Login by using CrmLogin.
        /// </summary>
        public RelayCommand ReloadCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    IsLoading = true;

                    // Download metadata
                    LoadMessage = "Downloading Metadata....";
                    await DownloadMetadata();

                    // Then generate context code
                    LoadMessage = "Generating Context....";
                    await Task.Run(() => LoadData());

                    // Finally set context
                    contextName = "System";

                    // Set the assembly name and Context class name.
                    props._cxInfo.CustomTypeInfo.CustomTypeName = String.Format("Microsoft.Dynamics.CRM.{0}", contextName);

                    IsLoaded = true;
                    IsLoading = false;
                });
            }
        }

        #endregion
    }
}
