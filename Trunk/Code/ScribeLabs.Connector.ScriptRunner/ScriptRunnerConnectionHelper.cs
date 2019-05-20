using System;
using System.Collections.Generic;

using Scribe.Core.ConnectorApi.Exceptions;
using Scribe.Core.ConnectorApi.ConnectionUI;
using Scribe.Core.ConnectorApi.Cryptography;

namespace ScribeLabs.Connector.ScriptRunner
{

    internal static class ScriptRunnerConnectionHelper
    {
        internal class ConnectionProperties
        {
            public bool Impersonate { get; set; }
            public string Domainname { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        #region Constants

        internal static class ConnectionPropertyKeys
        {
            public const string RunAs = "RunAs";
            public const string Username = "Username";
            public const string Password = "Password";
        }

        internal static class ConnectionPropertyLabels
        {
            public const string RunAs = "Run as";
            public const string Username = "Username";
            public const string Password = "Password";
        }

        internal static class RunAsTypes
        {
            public const string User = "User";
            public const string Agent = "Agent";
        }

        internal static class RunAsTypeLabels
        {
            public const string User = "User (Enter creds below)";
            public const string Agent = "Agent";
        }

        private const string HelpLink = "https://help.scribesoft.com/scribe/en/index.htm#sol/conn/ScriptRunner.htm";

        #endregion

        internal static ConnectionProperties GetConnectionProperties(IDictionary<string, string> propDictionary)
        {
            if (propDictionary == null)
                throw new InvalidConnectionException("Connection Properties are NULL");

            var connectorProps = new ConnectionProperties();

            var runAs = getRequiredPropertyValue(propDictionary, ConnectionPropertyKeys.RunAs, ConnectionPropertyLabels.RunAs);

            if (runAs == RunAsTypes.User || runAs == RunAsTypeLabels.User) // need to check for label due to UI caching issue?
            {
                var fullUserName = getRequiredPropertyValue(propDictionary, ConnectionPropertyKeys.Username, ConnectionPropertyLabels.Username);

                string userName;
                string domainName;
                parseUserAndDomainName(fullUserName, out userName, out domainName);

                connectorProps.Impersonate = true;
                connectorProps.Domainname = domainName;
                connectorProps.Username = userName;
                connectorProps.Password = getRequiredPropertyValue(propDictionary, ConnectionPropertyKeys.Password, ConnectionPropertyLabels.Password);
                connectorProps.Password = Decryptor.Decrypt_AesManaged(connectorProps.Password, ScriptRunnerConnector.CryptoKey);

                // re-check unencrypted password
                if (string.IsNullOrEmpty(connectorProps.Password))
                    throw new InvalidConnectionException(string.Format("A value is required for '{0}'", ConnectionPropertyLabels.Password));
            }
            else if (runAs == RunAsTypes.Agent)
            {
                connectorProps.Impersonate = false;
                connectorProps.Domainname = "";
                connectorProps.Username = "";
                connectorProps.Password = "";
            }
            else
                throw new InvalidConnectionException(string.Format("Invalid value for '{0}', must be {1} or {2}", 
                                                                                    ConnectionPropertyLabels.RunAs, RunAsTypes.Agent, RunAsTypes.User));

            return connectorProps;
        }

        private static string getRequiredPropertyValue(IDictionary<string, string> properties, string key, string label)
        {
            var value = getPropertyValue(properties, key);
            if (string.IsNullOrEmpty(value))
                throw new InvalidConnectionException(string.Format("A value is required for '{0}'", label));

            return value;
        }

        private static string getPropertyValue(IDictionary<string, string> properties, string key)
        {
            var value = "";
            properties.TryGetValue(key, out value);
            return value;
        }

        internal static FormDefinition GetConnectionFormDefintion()
        {

            var formDefinition = new FormDefinition
            {
                CompanyName = ScriptRunnerConnector.ConnectorTypeName,
                CryptoKey = ScriptRunnerConnector.CryptoKey,
                HelpUri = new Uri(HelpLink)
            };

            formDefinition.Add(BuildRunAsDefinition(0));
            formDefinition.Add(BuildUsernameDefinition(1));
            formDefinition.Add(BuildPasswordDefinition(2));

            return formDefinition;
        }

        #region Form Definition Builders

        private static EntryDefinition BuildRunAsDefinition(int order)
        {
            var entryDefinition = new EntryDefinition
            {
                InputType = InputType.Text,
                IsRequired = true,
                Label = ConnectionPropertyLabels.RunAs,
                PropertyName = ConnectionPropertyKeys.RunAs,
                Order = order,
            };

            entryDefinition.Options.Add(RunAsTypeLabels.User, RunAsTypes.User);
            entryDefinition.Options.Add(RunAsTypeLabels.Agent, RunAsTypes.Agent);

            return entryDefinition;
        }

        private static EntryDefinition BuildUsernameDefinition(int order)
        {
            var entryDefinition = new EntryDefinition
            {
                InputType = InputType.Text,
                IsRequired = false,
                Label = ConnectionPropertyLabels.Username,
                PropertyName = ConnectionPropertyKeys.Username,
                Order = order,
            };

            return entryDefinition;
        }

        private static EntryDefinition BuildPasswordDefinition(int order)
        {
            var entryDefinition = new EntryDefinition
            {
                InputType = InputType.Password,
                IsRequired = false,
                Label = ConnectionPropertyLabels.Password,
                PropertyName = ConnectionPropertyKeys.Password,
                Order = order,
            };

            return entryDefinition;
        }


        #endregion

        private static void parseUserAndDomainName(string userNameAndDomainName, out string userName, out string domainName)
        {
            domainName = string.Empty;

            if (userNameAndDomainName.IndexOf("\\") > -1)
            {
                domainName = userNameAndDomainName.Split(new[] { '\\' })[0];
                userName = userNameAndDomainName.Split(new[] { '\\' })[1];
            }
            else if (userNameAndDomainName.IndexOf("@") > -1)
            {
                domainName = userNameAndDomainName.Split(new[] { '@' })[1];
                userName = userNameAndDomainName.Split(new[] { '@' })[0];
            }
            else
            {
                userName = userNameAndDomainName;
            }
        }
    }
}
