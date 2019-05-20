using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Scribe.Core.ConnectorApi;
using Scribe.Core.ConnectorApi.Query;
using Scribe.Core.ConnectorApi.Actions;


namespace ScribeLabs.Connector.ScriptRunner.Test
{
    [TestClass]
    public class OperationUnitTests : ConnectedOperationUnitTestBase
    {
        #region setup and tear down of test class

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            setup(context);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            cleanup();
        }

        #endregion


        [TestMethod]
        public void OP_Query()
        {
            var script = @"Scripts\EchoTime.cmd";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var query = new Query
            {
                RootEntity = new QueryEntity
                {
                    ObjectDefinitionFullName = ScriptRunnerService.ObjectNames.EnvironmentInfo,
                    Name = "Script",
                    PropertyList =
                    {
                        ScriptRunnerService.EnvironmentInfoObjPropNames.AgentMachine,
                        ScriptRunnerService.EnvironmentInfoObjPropNames.AgentTimezone,
                        ScriptRunnerService.EnvironmentInfoObjPropNames.RunningAs,
                        ScriptRunnerService.EnvironmentInfoObjPropNames.StartTimeUTC
                    }
                }
            };

            var enumerator = connector.ExecuteQuery(query);
            Assert.IsNotNull(enumerator);

            var results = enumerator.ToList();
            Assert.IsNotNull(results);

            Assert.IsTrue(results.Count == 1);
        }

        [TestMethod]
        public void OP_Execute_CMD()
        {

            var script = @"Scripts\EchoTime.cmd";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Cmd.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "True" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        public void OP_Execute_CMD_WithArgs()
        {

            var script = @"Scripts\EchoArgs.cmd";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Cmd.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ScriptArguments, "Marco Polo"},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        public void OP_Execute_CMD_ForExe()
        {

            var script = @"Scripts\RunExec.cmd";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Cmd.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "False" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        public void OP_Execute_BAT()
        {

            var script = @"Scripts\EchoTime.BAT";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Cmd.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        public void OP_Execute_PS1()
        {

            var script = @"Scripts\EchoTime.ps1";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Powershell.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        [Ignore] //python required
        public void OP_Execute_PY()
        {
            var script = @"Scripts\EchoTime.py";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Python.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        [Ignore] //python required
        public void OP_Execute_PYW()
        {

            var script = @"Scripts\EchoTime.pyw";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Pythonw.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectSuccess(results);
        }

        [TestMethod]
        public void OP_Execute_Failure_ExeAsInterpreter()
        {

            var script = @"Scripts\EchoTime.bat";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Notepad.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectError(results);
        }

        [TestMethod]
        public void OP_Execute_Failure_ExeAsScript()
        {

            var script = @"Scripts\EchoTimeFake.exe";
            var dir = Directory.GetCurrentDirectory();
            script = Path.Combine(dir, script);

            var operationInput = new OperationInput
            {
                Name = "Execute",
                Input = new DataEntity[]
                {
                    new DataEntity(ScriptRunnerService.ObjectNames.Script)
                    {
                        Properties =
                        {
                            {ScriptRunnerService.ScriptObjPropNames.Interpreter, "Cmd.exe"},
                            {ScriptRunnerService.ScriptObjPropNames.Script, script},
                            {ScriptRunnerService.ScriptObjPropNames.ProcessWindowStyle, "Hidden" },
                            {ScriptRunnerService.ScriptObjPropNames.WaitForExit, "true" }
                        }
                    }
                }
            };

            var results = connector.ExecuteOperation(operationInput);

            ExpectError(results);
        }

    }
}
