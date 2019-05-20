using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Exceptions;
using Scribe.Core.ConnectorApi.Cryptography;

namespace ScribeLabs.Connector.ScriptRunner.Test
{
    [TestClass]
    public class ConnectorUnitTests
    {
        public static Dictionary<string, string> GetTestConnectionDictionary()
        {
            var connectionDictionary = new Dictionary<string, string>
            {
                {ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs, ScriptRunnerConnectionHelper.RunAsTypes.Agent},
                {ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Username, ""},
                {ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Password, ""},
            };

            return connectionDictionary;
        }

        [TestMethod]
        public void CON_Construct()
        {
            IConnector connector = new ScriptRunnerConnector();

            Assert.IsNotNull(connector);
        }

        [TestMethod]
        public void CON_Preconnect()
        {
            IConnector connector = new ScriptRunnerConnector();

            var result = connector.PreConnect(null);

            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        [TestMethod]
        public void CON_Connect_AsAgent()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            connector.Connect(props);

            Assert.IsTrue(connector.IsConnected);

            if (connector.IsConnected)
                connector.Disconnect();
        }

        [TestMethod]
        public void CON_Connect_AsUser_Success()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            // needs valid creds
            var password = Encryptor.Encrypt_AesManaged("myPasswordHere", ScriptRunnerConnector.CryptoKey);
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs] = ScriptRunnerConnectionHelper.RunAsTypes.User;
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Username] = "nkeefe";
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Password] = password;

            connector.Connect(props);

            Assert.IsTrue(connector.IsConnected);

            if (connector.IsConnected)
                connector.Disconnect();
        }

        [TestMethod]
        public void CON_Connect_AsUser_Failure_NoUserOrPassword()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs] = ScriptRunnerConnectionHelper.RunAsTypes.User;

            try
            {
                connector.Connect(props);

                Assert.Fail("Should have thrown a InvalidConnectionException");
            }
            catch (InvalidConnectionException ex)
            {
            }

            Assert.IsFalse(connector.IsConnected);

            if (connector.IsConnected)
                connector.Disconnect();
        }

        [TestMethod]
        public void CON_Connect_AsUser_Failure_ValidUserNoPassword()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs] = ScriptRunnerConnectionHelper.RunAsTypes.User;
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Username] = "crasher";
            var password = Encryptor.Encrypt_AesManaged("", ScriptRunnerConnector.CryptoKey);
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Password] = password;

            try
            {
                connector.Connect(props);

                Assert.Fail("Should have thrown a InvalidConnectionException");
            }
            catch (InvalidConnectionException ex)
            {
            }

            Assert.IsFalse(connector.IsConnected);

            if (connector.IsConnected)
                connector.Disconnect();
        }

        //
        // Will lock you out!!!
        //
        //[TestMethod]
        //public void CON_Connect_AsUser_Failure_CurrentUserBadPassword()
        //{
        //    IConnector connector = new ScriptRunnerConnector();

        //    var props = GetTestConnectionDictionary();

        //    props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs] = ScriptRunnerConnectionHelper.RunAsTypes.User;
        //    props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Username] = Environment.UserDomainName + "\\" + Environment.UserName;
        //    var password = Encryptor.Encrypt_AesManaged("bad-password", ScriptRunnerConnector.CryptoKey);
        //    props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Password] = password;

        //    try
        //    {
        //        connector.Connect(props);

        //        Assert.Fail("Should have thrown a InvalidConnectionException");
        //    }
        //    catch (InvalidConnectionException ex)
        //    {
        //    }

        //    Assert.IsFalse(connector.IsConnected);

        //    if (connector.IsConnected)
        //        connector.Disconnect();
        //}

        [TestMethod]
        public void CON_Connect_AsUser_Failure_BadUser()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.RunAs] = ScriptRunnerConnectionHelper.RunAsTypes.User;
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Username] = "bad-user";
            var password = Encryptor.Encrypt_AesManaged("bad-password", ScriptRunnerConnector.CryptoKey);
            props[ScriptRunnerConnectionHelper.ConnectionPropertyKeys.Password] = password;

            try
            {
                connector.Connect(props);

                Assert.Fail("Should have thrown a InvalidConnectionException");
            }
            catch (InvalidConnectionException ex)
            {
                if (ex.Message != "The user name or password is incorrect" &&
                    ex.Message != "There are currently no logon servers available to service the logon request")
                {
                    Assert.Fail("Threw wrong InvalidConnectionException: " + ex.Message);
                }
            }

            Assert.IsFalse(connector.IsConnected);

            if (connector.IsConnected)
                connector.Disconnect();
        }

        [TestMethod]
        public void CON_Disconnect()
        {
            IConnector connector = new ScriptRunnerConnector();

            connector.Disconnect();

        }

        [TestMethod]
        public void CON_GetMetadataProvider()
        {
            IConnector connector = new ScriptRunnerConnector();

            var props = GetTestConnectionDictionary();

            connector.Connect(props);

            Assert.IsTrue(connector.IsConnected);

            var metadataProvider = connector.GetMetadataProvider();

            Assert.IsNotNull(metadataProvider);

            if (connector.IsConnected)
                connector.Disconnect();
        }

    }
}
