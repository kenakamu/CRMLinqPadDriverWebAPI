# Project Description 

This is LINQPad driver which enables you to connect to your Microsoft Dynamics CRM Web API and run LINQ queries.

## How to use this
Please see [HowTo.md](HowTo.md)<br/>
This driver supports Dynamics 365/CRM 2016 On-Premise, IFD and Online. Each has slightly different usage.

### On-Premise
If you are using On-Premise without Claim authentication, you can simply use Windows Authentication. So no additional work is required.

### IFD
If you are using IFD, then you need to manually register application first.

1. Log in to AD FS server (require Windows Server 2012 R2 AD FS)
2. Open PowerShell.
3, Run the following command to register application. You can change GUID as you need but you need to use "http://localhost/linqpad" as RedirectUri.
> Add-AdfsClient -ClientId 5ee98d47-38d1-4db5-b5c2-9a60f88c0076 -Name "CRM For LINQPad" -RedirectUri http://localhost/linqpad
4. Now you can add connection to LINQPad. Pass the ClientId before Login to CRM, and make use to select IFD for authentication.
5. You will be prompted when driver download metadata, and when you do query in new window.

### Online
For Online, the driver will automatically register your application. However, if you do not have admin privilege, you can register the application in different Azure AD and get Client Id. In that case, use following information to register.

1. Register as Native Application.
2. Give Dynamics CRM Online permission.
3. Use "http://localhost/linqpad" as RedirectUri

### Online with Consent
If you don't have enough privilege to register application to the Azure AD, you can register application in your own Azure AD and use the client id and redirect url for consent scenario.

The privilege the application needs are:
- Dynamics CRM Online : Access CRM Online as organization users
- Windows Azure Active Directory : Sign in and read user profile

Make sure to mark the application availableToOtherTenants to true. 
1. Download application manifest from "Manage Manifest" menu.
2. Open downloaded manifest with any editor.
3. Mark "true" for availableToOtherTenants.
4. Import the manifest back to the application.

#### Feedback
Please let us know what's working, what isn't, as well as suggestions for new capabilities by submitting a new issue

#### Connect
In addition to providing feedback on this project site, we'd love to hear directly from you! <br/> Please use Issues in this GitHub.

#### Additional Information
This solution uses Context generated from Web API endpoint metadata file displays corresponding OData query.
This driver does followings for you.

1. Let you Login to CRM by using common login window.
2. Register your application to Azure AD on your behalf. If CRM signed-in user does not have enough privilege, you will be prompted to signin as Azure AD Admin.
3. Download metadata file for you.
4. Generate context by using the metadata file and OData Client T4 Template.
