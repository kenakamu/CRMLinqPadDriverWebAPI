# Install the driver

1. Open LinqPad and click "Add connection".<br/>
![AddConnection.png](.\images\AddConnection.png)
1. Click "View more drivers". <br/>
![ViewMoreDrivers.png](.\images\ViewMoreDrivers.png)
1. Scroll down to find Dynamics CRM driver, then click "Download & Enable Driver". <br/>
![DynamicsDriver.png](.\images\DynamicsDriver.png)
1. Once download completed, you see the driver in your list.<br/>
![DynamicsDriverInList.png](.\images\DynamicsDriverInList.png)

# Connect and create the context
1. Select the installed driver and click "Next".<br/>
![DriverSelected.png](.\images\DriverSelected.png)
1. In the pop-up windows, you have several options. <br/>
![Top.png](.\images\Top.png)
1. If you already have an application registered to Azure AD, then uncheck the "Register to Azure AD automatically... " then you can enter your own ClientId. <br/>
![ClientId.png](.\images\ClientId.png)
1. If you want to use different version of API, check "Advanced Settings" and enter the API version. At the moment, v9 endpoint didn't work well, so enter "8.2" instead. <br/>
![APIVersion.png](.\images\APIVersion.png)
1. Click "Login to CRM" then signin dialog appears. Specify your credential and login. <br/>
![Login.png](.\images\Login.png)
1. Once login completed, wait until progress bar complets. It may ask you another credential to register an application to Azure AD if you let the driver register it on your behalf, then enter credential which has privilege to register an application to Azure AD.
1. When it download metadata and create context, you see the window below. Click Exit and wait until entites are loaded. It may take several seconds to minutes. <br/>
![Exit.png](.\images\Exit.png)
1. When everything done, you are ready to run LINQ query against your Dynamics instance.
<br/>
![Loaded.png](.\images\Loaded.png)

# Run LINQ query and see results.
1. From Connection dropdown, select a connection you want to execute the query by either press F5 or click green arrow.
<br/>
![SelectConnection.png](.\images\SelectConnection.png)
1. Enter LINQ query and execute. The results are shown in "Results pane"
<br/>
![AccountQuery.png](.\images\AccountQuery.png)

# Run C# statement.
1. Switch language from "C# Expression" to "C# statement". <br/>
![LanguageSelection.png](.\images\LanguageSelection.png)
1. Enter any statement you want to run and execute it.<br/>
![WhoAmI.png](.\images\WhoAmI.png)
1. Select "SQL" tab to see URI to run the query.<br/>
![WhoAmIUrl.png](.\images\WhoAmIUrl.png)

# Update the context.
1. When you need to update/refresh the context, then right click the connection and select "Refresh". <br/>
![Refresh.png](.\images\Refresh.png)
1. If you want to re-enter password or change setting, select Properties instead. <br/>
![Properties.png](.\images\Properties.png)
1. Select any option as you want. <br/>
![UpdateWindow.png](.\images\UpdateWindow.png)

Ken