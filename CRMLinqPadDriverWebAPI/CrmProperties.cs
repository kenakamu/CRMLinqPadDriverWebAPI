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
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.Pfe.Xrm
{
    /// <summary>
    /// Properties for this connection.
    /// </summary>
    class CrmProperties
    {
        public IConnectionInfo _cxInfo;
        readonly XElement _driverData;

        public CrmProperties(IConnectionInfo cxInfo)
        {
            _cxInfo = cxInfo;
            _driverData = cxInfo.DriverData;
        }
     
        public string OrgUri
        {
            get { return (string)_driverData.Element("OrgUri") ?? ""; }
            set { _driverData.SetElementValue("OrgUri", value); }
        }

        public string AuthenticationProviderType
        {
            get { return (string)_driverData.Element("AuthenticationProviderType") ?? ""; }
            set { _driverData.SetElementValue("AuthenticationProviderType", value); }
        }

        public string Authority
        {
            get { return (string)_driverData.Element("Authority") ?? ""; }
            set { _driverData.SetElementValue("Authority", value); }
        }

        public string Version
        {
            get { return (string)_driverData.Element("Version") ?? ""; }
            set { _driverData.SetElementValue("Version", value); }
        }

        public string DomainName
        {
            get { return (string)_driverData.Element("DomainName") ?? ""; }
            set { _driverData.SetElementValue("DomainName", value); }
        }

        public string UserName
        {
            // Encrypt/Decrypt data.
            get { return DecryptString((string)_driverData.Element("UserName")) ?? ""; }
            set { _driverData.SetElementValue("UserName", EncryptString(value)); }
        }

        public string Password
        {
            // Encrypt/Decrypt data.
            get { return DecryptString((string)_driverData.Element("Password")) ?? ""; }
            set { _driverData.SetElementValue("Password", EncryptString(value)); }
        }

        public string FriendlyName
        {
            get { return (string)_driverData.Element("FriendlyName") ?? ""; }
            set { _driverData.SetElementValue("FriendlyName", value); }
        }

        public string ClientId
        {
            get { return (string)_driverData.Element("ClientId") ?? ""; }
            set { _driverData.SetElementValue("ClientId", value); }
        }

        public string RedirectUri
        {
            get { return (string)_driverData.Element("RedirectUri") ?? ""; }
            set { _driverData.SetElementValue("RedirectUri", value); }
        }

        /// <summary>
        /// Encrypt string values
        /// </summary>
        /// <param name="value">original string</param>
        /// <returns>encrypted string</returns>
        private string EncryptString(string value)
        {
            // Read string into Byte Array
            byte[] byteValue = Encoding.UTF8.GetBytes(value);
            // Encrypt the value. You can pass optionalEntropy if you want.
            byte[] encryptedValue = ProtectedData.Protect(byteValue, null, DataProtectionScope.CurrentUser);
            // Return string value.
            return System.Convert.ToBase64String(encryptedValue);
        }

        /// <summary>
        /// Decrypt encrypted string
        /// </summary>
        /// <param name="value">encrypted string</param>
        /// <returns>decrypted string</returns>
        private string DecryptString(string value)
        {
            // Read string into Byte Array
            byte[] encryptedValue = System.Convert.FromBase64String(value);
            // Decrypt the value. If you use optionalEntropy when encrypt data, then you should pass same value here
            byte[] byteValue = ProtectedData.Unprotect(encryptedValue, null, DataProtectionScope.CurrentUser);
            // Return string value
            return System.Text.Encoding.UTF8.GetString(byteValue);
        }
    }
}
