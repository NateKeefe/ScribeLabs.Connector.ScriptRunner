using System;
using System.Collections.Generic;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Actions;
using Scribe.Core.ConnectorApi.Query;
using Scribe.Core.ConnectorApi.Exceptions;
using Scribe.Core.ConnectorApi.Logger;

using Scribe.Connector.Common;


namespace ScribeLabs.Connector.ScriptRunner
{
    #region Connector Attributes
    [ScribeConnector(
    ConnectorTypeIdAsString,
    ConnectorTypeName,
    ConnectorTypeDescription,
    typeof(ScriptRunnerConnector),
    "", // SettingsUITypeName (obsolete)
    "", // SettingsUIVersion (obsolete)
    ConnectionUITypeName,
    ConnectionUIVersion,
    "", // XapFileName (obsolete)
    new[] { "Scribe.IS2.Source", "Scribe.IS2.Target", "Scribe.MS2.Source", "Scribe.MS2.Target" },
    SupportsCloud,
    ConnectorVersion)]
    #endregion

    public class ScriptRunnerConnector : IConnector, IDisposable
    {
        #region Constants

        internal const string ConnectorTypeName = "Scribe Labs - ScriptRunner";

        internal const string ConnectorTypeDescription = "Scribe Labs ScriptRunner Connector";

        internal const string ConnectorVersion = "1.0.0.1";

        internal const string ConnectorTypeIdAsString = "ED1E0489-CE46-4477-B641-7714E6BC0579";

        internal const string CryptoKey = "ECFF1D82-F8A0-41FE-B145-1E9DB770825A";

        internal const bool SupportsCloud = false;

        internal const string ConnectionUITypeName = "ScribeOnline.GenericConnectionUI";

        internal const string ConnectionUIVersion = "1.0";

        #endregion

        private MethodInfo methodInfo;

        private ScriptRunnerService service;
        private IMetadataProvider metadataProvider;

        public ScriptRunnerConnector()
        {
            clearLocals();

            methodInfo = new MethodInfo(GetType().Name);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // free managed resources  
            clearLocals();
        }

        #region IConnector implimentation

        public Guid ConnectorTypeId => Guid.Parse(ConnectorTypeIdAsString);

        public bool IsConnected
        {
            get
            {
                if (service == null)
                    return false;

                return service.IsConnected;
            }
        }

        public string PreConnect(IDictionary<string, string> properties)
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    var uiDef = ScriptRunnerConnectionHelper.GetConnectionFormDefintion();
                    return uiDef.Serialize();
                }
                catch (Exception exception)
                {
                    unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return "";
        }

        public void Connect(IDictionary<string, string> properties)
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    // validate & get connection properties
                    var connectionProps = ScriptRunnerConnectionHelper.GetConnectionProperties(properties);

                    service = new ScriptRunnerService();

                    service.Connect(connectionProps);

                    metadataProvider = new ScriptRunnerMetadataProvider(service);
                }
                catch (InvalidConnectionException)
                {
                    clearLocals();
                    throw;
                }
                catch (Exception exception)
                {
                    clearLocals();
                    unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }
        }


        public void Disconnect()
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    clearLocals();
                }
                catch (Exception exception)
                {
                    unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }
        }

        /// <summary>
        /// Gets the metadata provider.
        /// </summary>
        /// <returns>The object that inherits the IMetadataProvider interface</returns>
        public IMetadataProvider GetMetadataProvider()
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                if (metadataProvider == null)
                    throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                return metadataProvider;
            }
        }


        public IEnumerable<DataEntity> ExecuteQuery(Query query)
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    if (service == null || service.IsConnected == false)
                        throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                    if (query == null)
                        throw new ArgumentNullException("query");

                    return service.ExecuteQuery(query);
                }
                catch (InvalidExecuteQueryException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        public OperationResult ExecuteOperation(OperationInput input)
        {
            using (new LogMethodExecution(ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    if (service == null || service.IsConnected == false)
                        throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                    if (input == null)
                        throw new ArgumentNullException("input");

                    if (input.Input == null)
                        throw new ArgumentNullException("input.Input");

                    ScriptRunnerService.SupportedActions action;
                    if (!Enum.TryParse(input.Name, out action))
                        throw new InvalidExecuteOperationException("Unsupported operation: " + input.Name);

                    switch (action)
                    {
                        case ScriptRunnerService.SupportedActions.Execute:
                            return service.ExecuteExecute(input);

                        default:
                            throw new InvalidExecuteOperationException("Unsupported operation: " + input.Name);
                    }
                }
                catch (InvalidExecuteOperationException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        public MethodResult ExecuteMethod(MethodInput input)
        {
            throw new NotImplementedException();
        }


        #endregion

        private void clearLocals()
        {
            if (metadataProvider != null)
            {
                metadataProvider.Dispose();
                metadataProvider = null;
            }

            if (service != null)
            {
                service.Disconnect();
                service = null;
            }
        }

        internal static void unhandledExecptionHandler(string methodName, Exception exception)
        {
            var msg = string.Format("Unhandled exception caught in {0}: {1}\n\n", methodName, exception.Message);
            var details = string.Format("Details: {0}", exception.ToString());

            Logger.Write(Logger.Severity.Error, ConnectorTypeDescription, msg + details);

            throw new ApplicationException(msg, exception);
        }
    }
}
