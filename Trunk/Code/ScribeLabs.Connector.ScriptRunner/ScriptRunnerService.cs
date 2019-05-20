using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Actions;
using Scribe.Core.ConnectorApi.Query;
using Scribe.Core.ConnectorApi.Exceptions;
using Scribe.Core.ConnectorApi.Logger;
using Scribe.Core.ConnectorApi.Metadata;

using Scribe.Connector.Common;

namespace ScribeLabs.Connector.ScriptRunner
{
    class ScriptRunnerService
    {
        #region Constants

        public enum SupportedActions
        {
            Query,
            Execute
        }

        internal static class ObjectNames
        {
            public const string Script = "Script";
            public const string EnvironmentInfo = "EnvironmentInfo";
        }

        private static class ObjectDescriptions
        {
            public const string Script = "Used to execute a script.";
            public const string EnvironmentInfo = "Details about the current agent environment.";
        }

        internal static class ScriptObjPropNames
        {
            public const string Interpreter = "Interpreter";
            public const string Script = "Script";
            public const string ScriptArguments = "ScriptArguments";
            public const string Output = "Output";
            public const string ErrorInfo = "ErrorInfo";
            public const string LastExitCode = "ExitCode";
            public const string ProcessWindowStyle = "ProcessWindowStyle";
            public const string WaitForExit = "WaitForExit";
        }

        private static class ScriptObjPropDescriptions
        {
            public const string Interpreter = "Full path and filename of script interpeter.";
            public const string Script = "Full path and filename of script (.CMD, .BAT, .PS1, .PY, .PYW)";
            public const string ScriptArguments = "Arguments to be passed to the script";
            public const string Output = "Textual output of the script.";
            public const string ErrorInfo = "Textual output the error if the script fails.";
            public const string ExitCode = "Numeric value returned by the script on exit, typically 0 on success, non 0 on failure.";
            public const string ProcessWindowStyle = "Specifies how a new window should appear when the system starts a process (Normal, Hidden, Minimized, Maximized). For running EXE's, set to Hidden.";
            public const string WaitForExit = "Specifies if the Execute should wait for the program to exit. TRUE would return Output results. FALSE would not result Output Results. Use FALSE for executing EXEs.";
        }

        internal static class EnvironmentInfoObjPropNames
        {
            public const string StartTimeUTC = "StartTimeUTC";
            public const string AgentMachine = "AgentMachine";
            public const string AgentTimezone = "AgentTimezone";
            public const string RunningAs = "RunningScriptsAs";
        }

        internal static class EnvironmentInfoObjPropDescriptions
        {
            public const string StartTimeUTC = "Map start time in UTC.";
            public const string AgentMachine = "Agent machine name.";
            public const string AgentTimezone = "Agent machine timezone.";
            public const string RunningAs = "User scripts will be run under.";
        }
        private class ExecutionResult
        {
            public string Interpreter { get; set; }
            public string Script { get; set; }
            public string Output { get; set; }
            public string ErrorInfo { get; set; }
            public int ExitCode { get; set; }
            public string ProcessWindowStyle { get; set; }
            public bool WaitForExit { get; set; }
            public Exception Exception { get; set; }
        }

        private class SupportedInterpreters
        {
            public const string Cmd = "cmd.exe";
            public const string Powershell = "powershell.exe";
            public const string Python = "python.exe";
            public const string PythonW = "pythonw.exe";
        }

        private class SupportedProcessWindowStyle
        {
            public const string Normal = "Normal";
            public const string Hidden = "Hidden";
            public const string Minimized = "Minimized";
            public const string Maximized = "Maximized";

        }

        private enum SupportedScriptTypes
        {
            bat,
            cmd,
            ps1,
            py,
            pyw
        }
        #endregion

        public bool IsConnected { get; set; }

        private ScriptRunnerConnectionHelper.ConnectionProperties connectionProps;

        public void Connect(ScriptRunnerConnectionHelper.ConnectionProperties connectionProps)
        {
            try
            {
                // this using will validate the creds if impersonating
                using (new Impersonator(connectionProps.Impersonate, connectionProps.Domainname, connectionProps.Username, connectionProps.Password))
                {
                    if(Environment.UserName.ToUpper() == "SYSTEM")
                        throw new InvalidConnectionException("Running as agent is not supported if the agent is running as SYSTEM");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new InvalidConnectionException(ex);
            }

            this.connectionProps = connectionProps;

            IsConnected = true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }


        public IEnumerable<IActionDefinition> RetrieveActionDefinitions()
        {
            //Convert the operations enum to a list of string values
            var supportedActions = Enum.GetValues(typeof(SupportedActions)).Cast<SupportedActions>();

            //Parse through 
            foreach (var supportedAction in supportedActions)
            {
                var actionDefintion = new ActionDefinition { SupportsMultipleRecordOperations = false };

                actionDefintion.Name = supportedAction.ToString();
                actionDefintion.FullName = supportedAction.ToString();
                actionDefintion.Description = string.Empty;

                switch (supportedAction)
                {
                    case SupportedActions.Query:
                        actionDefintion.SupportsLookupConditions = false;
                        actionDefintion.SupportsBulk = false;
                        actionDefintion.SupportsConstraints = true;
                        actionDefintion.SupportsRelations = false;
                        actionDefintion.SupportsSequences = false;
                        actionDefintion.KnownActionType = KnownActions.Query;
                        break;

                    case SupportedActions.Execute:
                        actionDefintion.SupportsLookupConditions = false;
                        actionDefintion.SupportsBulk = false;
                        actionDefintion.SupportsConstraints = false;
                        actionDefintion.SupportsRelations = false;
                        actionDefintion.SupportsSequences = false;
                        actionDefintion.KnownActionType = KnownActions.Create;
                        break;

                    default:
                        throw new ApplicationException("Unsuported action defined: " + supportedAction.ToString());
                }

                yield return actionDefintion;
            }
        }

        public IObjectDefinition RetrieveObjectDefinition(string objectName, bool shouldGetProperties = false, bool shouldGetRelations = false)
        {

            switch (objectName)
            {
                case ObjectNames.Script:
                    return buildScriptObjectDef(shouldGetProperties);

                case ObjectNames.EnvironmentInfo:
                    return buildEnvironmentInfoObjectDef(shouldGetProperties);
            }

            return null;
        }

        public IEnumerable<IObjectDefinition> RetrieveObjectDefinitions(bool shouldGetProperties = false, bool shouldGetRelations = false)
        {
            var objectDefs = new List<IObjectDefinition>();

            objectDefs.Add(buildScriptObjectDef(shouldGetProperties));
            objectDefs.Add(buildEnvironmentInfoObjectDef(shouldGetProperties));

            return objectDefs;
        }

        private IObjectDefinition buildScriptObjectDef(bool shouldGetProperties = false)
        {
            var objectDef = new ObjectDefinition()
            {
                Name = ObjectNames.Script,
                FullName = ObjectNames.Script,
                Hidden = false,
                SupportedActionFullNames = new List<string>() { SupportedActions.Execute.ToString() },
                Description = ObjectDescriptions.Script,
            };

            if (shouldGetProperties)
            {
                objectDef.PropertyDefinitions = new List<IPropertyDefinition>();
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, true, ScriptObjPropNames.Interpreter, "System.String", 256, ScriptObjPropDescriptions.Interpreter));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, true, ScriptObjPropNames.Script, "System.String", 256, ScriptObjPropDescriptions.Script));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, true, ScriptObjPropNames.ScriptArguments, "System.String", 256, ScriptObjPropDescriptions.ScriptArguments));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, true, ScriptObjPropNames.ProcessWindowStyle, "System.String", 9, ScriptObjPropDescriptions.ProcessWindowStyle));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, true, ScriptObjPropNames.WaitForExit, "System.String", 5, ScriptObjPropDescriptions.WaitForExit));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, true, false, ScriptObjPropNames.Output, "System.String", 0, ScriptObjPropDescriptions.Output));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, true, false, ScriptObjPropNames.ErrorInfo, "System.String", 0, ScriptObjPropDescriptions.ErrorInfo));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, true, false, ScriptObjPropNames.LastExitCode, "System.Int32", 0, ScriptObjPropDescriptions.ExitCode));
            }

            return objectDef;
        }

        private IObjectDefinition buildEnvironmentInfoObjectDef(bool shouldGetProperties = false)
        {
            var objectDef = new ObjectDefinition()
            {
                Name = ObjectNames.EnvironmentInfo,
                FullName = ObjectNames.EnvironmentInfo,
                Hidden = false,
                SupportedActionFullNames = new List<string>() { SupportedActions.Query.ToString() },
                Description = ObjectDescriptions.EnvironmentInfo,
            };

            if (shouldGetProperties)
            {
                objectDef.PropertyDefinitions = new List<IPropertyDefinition>();
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, false, EnvironmentInfoObjPropNames.AgentMachine, "System.String", 256, EnvironmentInfoObjPropDescriptions.AgentMachine));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, false, EnvironmentInfoObjPropNames.AgentTimezone, "System.String", 25, EnvironmentInfoObjPropDescriptions.AgentTimezone));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(false, false, false, EnvironmentInfoObjPropNames.RunningAs, "System.String", 128, EnvironmentInfoObjPropDescriptions.RunningAs));
                objectDef.PropertyDefinitions.Add(buildPropertyDef(true, false, false, EnvironmentInfoObjPropNames.StartTimeUTC, "System.DateTime", 0, EnvironmentInfoObjPropDescriptions.StartTimeUTC));
            }

            return objectDef;
        }

        public IEnumerable<DataEntity> ExecuteQuery(Query query)
        {
            if (query.RootEntity == null)
                throw new ArgumentNullException("query.RootEntity");

            if (query.RootEntity.ObjectDefinitionFullName != ObjectNames.EnvironmentInfo)
                throw new ApplicationException("Execute does not support object type: " + query.RootEntity.ObjectDefinitionFullName);

            // package the result
            var output = new List<DataEntity>();
            output.Add(EnvironmentInfoToDataEntity());
            return output;
        }

        public OperationResult ExecuteExecute(OperationInput input)
        {
            if (input.Input.Length != 1)
                throw new ApplicationException("Execute does not support multi-record operations");

            if (input.Input[0].ObjectDefinitionFullName != ObjectNames.Script)
                throw new ApplicationException("Execute does not support object type: " + input.Input[0].ObjectDefinitionFullName);

            if (input.Input[0].Properties == null)
                throw new ArgumentNullException("input.Input[0].Properties");

            // parse the input for interpreter and script
            var inputDE = input.Input[0];
            if(inputDE.Properties.ContainsKey(ScriptObjPropNames.Interpreter) == false)
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.Interpreter);

            var interpreter = inputDE.Properties[ScriptObjPropNames.Interpreter].ToString();
            if (string.IsNullOrWhiteSpace(interpreter))
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.Interpreter);

            var processWindowStyle = inputDE.Properties[ScriptObjPropNames.ProcessWindowStyle].ToString();
            if (string.IsNullOrWhiteSpace(processWindowStyle))
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.ProcessWindowStyle);

            var waitForExit = inputDE.Properties[ScriptObjPropNames.WaitForExit].ToString();
            if (string.IsNullOrWhiteSpace(waitForExit))
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.WaitForExit);

            if (inputDE.Properties.ContainsKey(ScriptObjPropNames.Script) == false)
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.Script);

            var script = inputDE.Properties[ScriptObjPropNames.Script].ToString();
            if (string.IsNullOrWhiteSpace(interpreter))
                throw new ApplicationException("Required property not set: " + ScriptObjPropNames.Script);

            var scriptArguments = "";
            if(inputDE.Properties.ContainsKey(ScriptObjPropNames.ScriptArguments))
                scriptArguments = inputDE.Properties[ScriptObjPropNames.ScriptArguments].ToString();

            // execute the script
            var executionResult = executeCommand(interpreter, script, scriptArguments, true, processWindowStyle, waitForExit);

            // package the results
            var outputDE = ExecutionResultToDataEntity(ObjectNames.Script, interpreter, script, scriptArguments, processWindowStyle, waitForExit, executionResult);
            var success = true;
            var objectsAffected = 1;
            ErrorResult errorResult = null;

            if (executionResult.Exception != null)
            {
                success = false;
                objectsAffected = 0;
                errorResult = new ErrorResult();
                errorResult.Description = executionResult.Exception.Message;
                errorResult.Detail = executionResult.Exception.ToString();
            }

            var result = new OperationResult();
            result.Output = new DataEntity[] { outputDE };
            result.Success = new bool[] { success };
            result.ObjectsAffected = new int[] { objectsAffected };
            result.ErrorInfo = new ErrorResult[] { errorResult };

            return result;
        }

        private string validateInterpreter(string interpreter)
        {
            if (isSupportedInterpreter(interpreter, SupportedInterpreters.Cmd))
                return interpreter;

            if (isSupportedInterpreter(interpreter, SupportedInterpreters.Powershell))
                return interpreter;

            if (isSupportedInterpreter(interpreter, SupportedInterpreters.Python))
                return interpreter;

            if (isSupportedInterpreter(interpreter, SupportedInterpreters.PythonW))
                return interpreter;

            throw new ApplicationException("Unsupported script interpreter: " + interpreter);
        }

        private bool isSupportedInterpreter(string interpreter, string supportedInterpreter)
        {
            interpreter = interpreter.ToLower();

            // Exact match is OK
            if (interpreter == supportedInterpreter)
                return true;

            // If we match at end of a path, make sure it's a real file
            if (interpreter.EndsWith(supportedInterpreter))
            {
                if (File.Exists(interpreter))
                    return true;

                throw new ApplicationException("Script interpreter not found: " + interpreter);
            }

            return false;
        }

        private string validateScript(string interpreter, string script)
        {
            // make sure it's a real file
            if (!File.Exists(script))
                throw new ApplicationException("Script not found: " + script);

            // validate script extention
            var scriptExtension = Path.GetExtension(script.ToLower());
            scriptExtension = scriptExtension.Replace(".", "");
            SupportedScriptTypes scriptType;
            if (!Enum.TryParse(scriptExtension, out scriptType))
                throw new ApplicationException("Unsupported script type: *." + scriptExtension);

            // add any interpreter specific arguments
            if (isSupportedInterpreter(interpreter, SupportedInterpreters.Cmd))
            {
                script = " /c " + script;
            }

            return script;
        }

        private ExecutionResult executeCommand(string interpreter, string script, string scriptArguments, bool isScriptCommand, string processWindowStyle, string waitForExit, int msTimeout = -1)
        {
            var result = new ExecutionResult();
            using (new Impersonator(connectionProps.Impersonate, connectionProps.Domainname, connectionProps.Username, connectionProps.Password))
            {
                var command = "";
                bool exit = waitForExit.ToLower() == "true";

                try
                {
                    interpreter = interpreter?.Trim();
                    script = script?.Trim();
                    scriptArguments = scriptArguments?.Trim();

                    command = string.Format("{0} {1} {2}", interpreter, script, scriptArguments);

                    Logger.Write(Logger.Severity.Info, ScriptRunnerConnector.ConnectorTypeDescription, "Executing Command: " + command);

                    if (isScriptCommand)
                    {
                        interpreter = validateInterpreter(interpreter);
                        script = validateScript(interpreter, script);
                    }

                    if(!string.IsNullOrWhiteSpace(scriptArguments))
                    {
                        script = string.Format("{0} {1}", script, scriptArguments);
                    }

                    if (string.IsNullOrWhiteSpace(command))
                    {
                        // empty command
                        throw new ArgumentNullException("command");
                    }
                    else
                    {
                        // execute the command
                        var startInfo = new ProcessStartInfo(interpreter, script);

                        startInfo.CreateNoWindow = true;
                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.RedirectStandardError = true;
                        startInfo.WindowStyle = (ProcessWindowStyle)Enum.Parse(typeof(ProcessWindowStyle), processWindowStyle); //added for EXEs

                        using (Process process = Process.Start(startInfo))
                        {
                            if (msTimeout == -1)
                                process.WaitForExit();
                            else
                                process.WaitForExit(msTimeout);

                            result.ExitCode = process.ExitCode;

                            if (exit)
                                using (StreamReader reader = process.StandardOutput)
                                {
                                    result.Output = reader.ReadToEnd();
                                }
                            if (exit)
                                using (StreamReader reader = process.StandardError)
                                {
                                    result.ErrorInfo = reader.ReadToEnd();
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if(ex.Message == "The system cannot find the file specified")
                        ex = new ApplicationException(ex.Message + ": " + interpreter, ex);
                    result.Exception = ex;
                }

                if (Logger.CheckLoggingSeverity(Logger.Severity.Debug))
                {
                    var msg = "Command execution results ";
                    msg += Environment.NewLine;
                    msg += string.Format("Command: {0} ", command);
                    msg += (Environment.NewLine + Environment.NewLine);
                    msg += string.Format("Output: {0} ", result.Output);
                    msg += (Environment.NewLine + Environment.NewLine);
                    msg += string.Format("ErrorInfo: {0} ", result.ErrorInfo);
                    msg += (Environment.NewLine + Environment.NewLine);
                    msg += string.Format("ExitCode: {0} ", result.ExitCode);
                    msg += (Environment.NewLine + Environment.NewLine);
                    msg += string.Format("Exception: {0} ", result.Exception == null ? "NULL" : result.Exception.ToString());
                    Logger.Write(Logger.Severity.Debug, ScriptRunnerConnector.ConnectorTypeDescription, msg);
                }
            }

            return result;
        }

        private DataEntity ExecutionResultToDataEntity(string objDefName, string interpreter, string script, string scriptArguments, string processWindowStyle, string waitForExit, ExecutionResult executionResult)
        {
            var dataEntity = new DataEntity(objDefName);
            dataEntity.Properties = new EntityProperties();
            dataEntity.Properties.Add(ScriptObjPropNames.Interpreter, interpreter);
            dataEntity.Properties.Add(ScriptObjPropNames.Script, script);
            dataEntity.Properties.Add(ScriptObjPropNames.ScriptArguments, scriptArguments);
            dataEntity.Properties.Add(ScriptObjPropNames.ProcessWindowStyle, processWindowStyle);
            dataEntity.Properties.Add(ScriptObjPropNames.WaitForExit, waitForExit);
            if (executionResult.Exception == null)
            {
                dataEntity.Properties.Add(ScriptObjPropNames.Output, executionResult.Output);
                dataEntity.Properties.Add(ScriptObjPropNames.ErrorInfo, executionResult.ErrorInfo);
                dataEntity.Properties.Add(ScriptObjPropNames.LastExitCode, executionResult.ExitCode);
            }
            else
            {
                dataEntity.Properties.Add(ScriptObjPropNames.Output, "");
                dataEntity.Properties.Add(ScriptObjPropNames.ErrorInfo, executionResult.Exception.Message);
                dataEntity.Properties.Add(ScriptObjPropNames.LastExitCode, -1);
            }
            return dataEntity;
        }


        private DataEntity EnvironmentInfoToDataEntity()
        {
            using (new Impersonator(connectionProps.Impersonate, connectionProps.Domainname, connectionProps.Username, connectionProps.Password))
            {
                var dataEntity = new DataEntity(ObjectNames.EnvironmentInfo);
                dataEntity.Properties = new EntityProperties();
                dataEntity.Properties.Add(EnvironmentInfoObjPropNames.AgentMachine, Environment.MachineName);
                dataEntity.Properties.Add(EnvironmentInfoObjPropNames.AgentTimezone, TimeZone.CurrentTimeZone.StandardName);
                dataEntity.Properties.Add(EnvironmentInfoObjPropNames.RunningAs, Environment.UserName);
                dataEntity.Properties.Add(EnvironmentInfoObjPropNames.StartTimeUTC, DateTime.Now.ToUniversalTime());
                return dataEntity;
            }
        }

        private PropertyDefinition buildPropertyDef(bool isPrimaryKey, bool nullable, bool usedInActionInput, 
                                                        string fullName, string dataType, int size, string description)
        {
            return new PropertyDefinition()
            {
                Name = fullName,
                FullName = fullName,
                UsedInQuerySelect = true,
                UsedInActionInput = usedInActionInput,
                UsedInActionOutput = true,
                PropertyType = dataType,
                Size = size,
                RequiredInActionInput = !nullable,
                Nullable = nullable,
                MaxOccurs = 1,
                MinOccurs = nullable ? 0 : 1,
                IsPrimaryKey = isPrimaryKey,
                Description = description,
            };
        }
    }
}
