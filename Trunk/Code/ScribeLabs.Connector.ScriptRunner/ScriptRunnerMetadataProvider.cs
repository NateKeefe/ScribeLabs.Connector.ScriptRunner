﻿using System;
using System.Collections.Generic;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Logger;
using Scribe.Core.ConnectorApi.Metadata;

using Scribe.Connector.Common;

namespace ScribeLabs.Connector.ScriptRunner
{
    internal class ScriptRunnerMetadataProvider: IMetadataProvider
    {
        private MethodInfo methodInfo;

        private ScriptRunnerService service;

        private ScriptRunnerMetadataProvider()
        {
            // hidden
        }

        internal ScriptRunnerMetadataProvider(ScriptRunnerService service)
        {
            methodInfo = new MethodInfo(GetType().Name);

            if (service == null || service.IsConnected == false)
                throw new ApplicationException("Must connect creating Metadata Provider");

            this.service = service;
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
            service = null;
        }

        void IMetadataProvider.ResetMetadata()
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                // No operation
            }
        }

        IEnumerable<IActionDefinition> IMetadataProvider.RetrieveActionDefinitions()
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {                
                try
                {
                    if (service == null || service.IsConnected == false)
                        throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                    return service.RetrieveActionDefinitions();
                }
                catch (Exception exception)
                {
                    ScriptRunnerConnector.unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        IObjectDefinition IMetadataProvider.RetrieveObjectDefinition(string objectName, bool shouldGetProperties, bool shouldGetRelations)
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    if (service == null || service.IsConnected == false)
                        throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                    return service.RetrieveObjectDefinition(objectName, shouldGetProperties, shouldGetRelations);
                }
                catch (Exception exception)
                {
                    ScriptRunnerConnector.unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        IEnumerable<IObjectDefinition> IMetadataProvider.RetrieveObjectDefinitions(bool shouldGetProperties, bool shouldGetRelations)
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    if (service == null || service.IsConnected == false)
                        throw new ApplicationException("Must connect before calling " + methodInfo.GetCurrentMethodName());

                    return service.RetrieveObjectDefinitions(shouldGetProperties, shouldGetRelations);
                }
                catch (Exception exception)
                {
                    ScriptRunnerConnector.unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        IMethodDefinition IMetadataProvider.RetrieveMethodDefinition(string objectName, bool shouldGetParameters)
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    // No operation
                    return null;
                }
                catch (Exception exception)
                {
                    ScriptRunnerConnector.unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }

        IEnumerable<IMethodDefinition> IMetadataProvider.RetrieveMethodDefinitions(bool shouldGetParameters)
        {
            using (new LogMethodExecution(ScriptRunnerConnector.ConnectorTypeDescription, methodInfo.GetCurrentMethodName()))
            {
                try
                {
                    // No operation
                    return new List<IMethodDefinition>();
                }
                catch (Exception exception)
                {
                    ScriptRunnerConnector.unhandledExecptionHandler(methodInfo.GetCurrentMethodName(), exception);
                }
            }

            return null;
        }
    }
}
