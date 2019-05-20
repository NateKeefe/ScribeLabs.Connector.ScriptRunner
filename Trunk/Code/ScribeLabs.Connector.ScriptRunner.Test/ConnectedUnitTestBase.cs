using Microsoft.VisualStudio.TestTools.UnitTesting;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Exceptions;

namespace ScribeLabs.Connector.ScriptRunner.Test
{
    [TestClass]
    public abstract class ConnectedUnitTestBase
    {
        protected static readonly IConnector connector = new ScriptRunnerConnector();


        #region setup and tear down of test class

        protected static void setup(TestContext context)
        {

            connector.Connect(ConnectorUnitTests.GetTestConnectionDictionary());

            if (connector.IsConnected == false)
            {
                throw new InvalidConnectionException("Unable to connect to Connector during ClassSetup");
            }

            var metadataProvider = connector.GetMetadataProvider();
            if (metadataProvider == null)
            {
                throw new InvalidConnectionException("Null MetadataProvider during ClassSetup");
            }
        }

        protected static void cleanup()
        {
            connector.Disconnect();
            if (connector.IsConnected)
            {
                throw new InvalidConnectionException("Unable to disconnect from Connector during ClassCleanup");
            }
        }

        #endregion
    }
}
