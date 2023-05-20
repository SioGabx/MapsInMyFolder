using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MapsInMyFolder.Commun
{
    public partial class Javascript
    {
        public enum JavascriptAction { refreshMap }
        public static event EventHandler<JavascriptAction> JavascriptActionEvent;

        #region logs
        //Register logger to the CustomOrEditPage
        public static Javascript JavascriptInstance = new Javascript();
        private string _logs;
        public class LogsEventArgs : EventArgs
        {
            public string Logs { get; set; }
        }
        public delegate void LogsChangedHandler(object source, LogsEventArgs e);
        public static event EventHandler<LogsEventArgs> LogsChanged;
        protected virtual void OnLogsChanged()
        {
            LogsChanged?.Invoke(this, new LogsEventArgs { Logs = _logs });
        }
        public string Logs
        {
            get { return _logs; }
            set
            {
                if (TileGeneratorSettings.AcceptJavascriptPrint)
                {
                    _logs = value;
                    OnLogsChanged();
                }
            }
        }
        #endregion

        private Dictionary<string, double> _Location;
        public bool ZoomToNewLocation;
        public class LocationEventArgs : EventArgs
        {
            public Dictionary<string, double> Location { get; set; }
        }
        public delegate void LocationChangedHandler(object source, LocationEventArgs e);
        public event EventHandler<LocationEventArgs> LocationChanged;
        protected virtual void OnLocationChanged()
        {
            LocationChanged?.Invoke(this, new LocationEventArgs());
        }
        public Dictionary<string, double> Location
        {
            get { return _Location; }
            set
            {
                _Location = value;
                if (_Location != null)
                {
                    OnLocationChanged();
                }
            }
        }

        public static long OldScriptTimestamp;
        public static readonly TimeSpan ScriptTimeOut = new TimeSpan(0,0,4);


        private static Engine SetupEngine(int LayerId)
        {
            Debug.WriteLine("New Settup Engine for layer" + LayerId);
            CancellationTokenSource JsCancelToken = new CancellationTokenSource();
            JsListCancelTocken.Remove(LayerId);
            JsListCancelTocken.Add(LayerId, JsCancelToken);

            Engine add = new Engine(options =>
            {
                options.TimeoutInterval(new TimeSpan(0, 1,0));
                options.MaxStatements(5000);
                options.CancellationToken(JsCancelToken.Token);
            });

            return SetupFunctions(add, LayerId);
        }

        public static Engine SetupFunctions(Engine engine, int LayerId)
        {
            engine.SetValue("print", (Action<object>)(stringtext => Javascript.Functions.Print(stringtext, LayerId)));
            engine.SetValue("printClear", (Action<object>)(_ => Javascript.Functions.PrintClear()));
            engine.SetValue("cls", (Action<object>)(_ => Javascript.Functions.PrintClear()));
            engine.SetValue("help", (Action<object>)(_ => Javascript.Functions.Help(LayerId)));
            engine.SetValue("setVar", (Func<object, object, bool, object>)((variable, value, isglobalvar) => Javascript.Functions.SetVar(variable, value, isglobalvar, LayerId)));
            engine.SetValue("getVar", (Func<object, object>)(variablename => Javascript.Functions.GetVar(variablename, LayerId)));
            engine.SetValue("clearVar", (Func<object, bool>)(variablename => Javascript.Functions.ClearVar(LayerId, variablename)));
            engine.SetValue("getTileNumber", (Func<object, object, object, object>)((latitude, longitude, zoom) => Javascript.Functions.CoordonneesToTile(latitude, longitude, zoom)));
            engine.SetValue("getLatLong", (Func<object, object, object, object>)((TileX, TileY, zoom) => Javascript.Functions.TileToCoordonnees(TileX, TileY, zoom)));
            engine.SetValue("setSelection", (Action<double, double, double, double, bool>)((NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation) =>
                Javascript.Functions.SetSelection(NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation, LayerId)));
            engine.SetValue("getSelection", (Func<object>)(() => Javascript.Functions.GetSelection()));
            engine.SetValue("alert", (Action<object, object>)((texte, caption) => Javascript.Functions.Alert(LayerId, texte, caption)));
            engine.SetValue("inputbox", (Func<object, object, object>)((texte, caption) => Javascript.Functions.InputBox(LayerId, texte, caption)));
            engine.SetValue("sendNotification", (Func<object, object, object, object, object>)((texte, caption, callback, notifId) =>
                Javascript.Functions.SendNotification(LayerId, texte, caption, callback, notifId)));
            engine.SetValue("refreshMap", (Func<object>)(() =>
            {
                Javascript.JavascriptActionEvent?.Invoke(LayerId, JavascriptAction.refreshMap);
                return null;
            }));

            return engine;
        }


        #region engines
        private static readonly Dictionary<int, CancellationTokenSource> JsListCancelTocken = new Dictionary<int, CancellationTokenSource>();
        private static readonly Dictionary<int, Engine> ListOfEngines = new Dictionary<int, Engine>();

        public static void EngineStopAll()
        {
            Javascript.setOldScriptTimestamp();
            foreach (KeyValuePair<int, CancellationTokenSource> tockensource in JsListCancelTocken)
            {
                Debug.WriteLine("Revoking" + tockensource.Key);
                CancelTokenSource(tockensource.Value);
            }
        }

        public static void EngineStopById(int LayerId)
        {
            if (JsListCancelTocken.TryGetValue(LayerId, out CancellationTokenSource tockensource))
            {
                CancelTokenSource(tockensource);
            }
        }

        public static void CancelTokenSource(CancellationTokenSource tockensource)
        {
            if (!tockensource.IsCancellationRequested)
            {
                tockensource.Cancel();
            }
        }

        static readonly object ExecuteScriptLocker = new object();
        public static Engine EngineGetById(int LayerId, string script)
        {
            if (ListOfEngines.TryGetValue(LayerId, out Engine engine))
            {
                return engine;
            }
            else
            {
                Engine add = SetupEngine(LayerId);
                try
                {
                    add = add.Execute(script);
                    if (CheckIfFunctionExist(add, Collectif.GetUrl.InvokeFunction.getTile.ToString()))
                    {
                        return add;
                    }
                    else
                    {
                        add = add.Execute(Settings.tileloader_default_script);
                        return add;
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Javascript.Functions.PrintError(ex.Message);
                }
                
            }
            return null;
        }

        public static void EngineDeleteById(int LayerId)
        {
            //lock (locker)
            //{
            ListOfEngines.Remove(LayerId);
            JsListCancelTocken.Remove(LayerId);
            //}
        }

        public static void EngineUpdate(Engine engine, int LayerId)
        {
            ListOfEngines[LayerId] = engine;
        }

        public static void EngineClearList()
        {
            Javascript.setOldScriptTimestamp();
            //lock (locker)
            //{
            ListOfEngines.Clear();
            JsListCancelTocken.Clear();
            //}
        }
        #endregion

        public static string AddOrReplaceFunction(string script, string functionName, string functionInstance)
        {
            var JavaScriptParser = new JavaScriptParser();
            Script ParsedScript = JavaScriptParser.ParseScript(script);
            string JoinStatements = string.Empty;
            bool FunctionFound = false;
            foreach (Statement IndividualStatement in ParsedScript.Body)
            {
                if (IndividualStatement is FunctionDeclaration functionDeclaration)
                {
                    if (functionDeclaration?.Id?.Name == functionName)
                    {
                        FunctionFound = true;
                        JoinStatements += functionInstance;
                    }
                    else
                    {
                        JoinStatements += IndividualStatement;
                    }
                }
            }
            if (!FunctionFound)
            {
                JoinStatements += functionInstance;
            }

            return JoinStatements;
        }

        public static bool CheckIfFunctionExist(Engine add, string functionName)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                return false;
            }
            JsValue evaluateValue = add.Evaluate("typeof " + functionName + " === 'function'");
            return evaluateValue.AsBoolean();
        }

        public static bool CheckIfFunctionExist(int LayerId, string functionName, string script = null)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                return false;
            }
            if (string.IsNullOrEmpty(script))
            {
                script = Layers.GetLayerById(LayerId).class_tilecomputationscript;
            }
            Engine add;
            //lock (locker)
            //{
                add = EngineGetById(LayerId, script);
            //}
            if (add == null || string.IsNullOrEmpty(functionName))
            {
                return false;
            }
            JsValue evaluateValue = add?.Evaluate("typeof " + functionName + " === 'function'") ?? false;

            return evaluateValue.AsBoolean();
        }
        public static Jint.Native.JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, Collectif.GetUrl.InvokeFunction InvokeFunction)
        {
            return ExecuteScript(script, arguments, LayerId, InvokeFunction.ToString());
        }
       
        public static Jint.Native.JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, string InvokeFunctionString)
        {
            long ScriptTimestamp = GetTimestamp();
            lock (ExecuteScriptLocker)
            {
                if (scriptIsOld(ScriptTimestamp))
                {
                    Functions.PrintError("L'éxécution de la function à été annulée car sa date de démarrage est trop ancienne et/ou à été révoquée");
                    return null;
                }

                Engine add = EngineGetById(LayerId, script);

                if (add is null)
                {
                    return null;
                }
                Jint.Native.JsValue jsValue = null;

                try
                {
                    //Debug.WriteLine("LAST INVOKE " + InvokeFunctionString + "  " + LayerId);
                    jsValue = add?.Invoke(InvokeFunctionString, arguments);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (ex.Message == "Can only invoke functions")
                    {
                        //PrintError("No main function fund. Use \"function getT(args) {}\"");
                        Javascript.Functions.PrintError("La fonction " + InvokeFunctionString + " n'as pas été trouvé dans le script. Faite help() pour obtenir de l'aide sur cette commande.");
                    }
                    else
                    {
                        Javascript.Functions.PrintError(ex.Message);
                    }
                    return null;
                }
                finally
                {
                    EngineUpdate(add, LayerId);
                }

                return jsValue;
            }
        }


        public static long GetTimestamp()
        {
            return DateTime.Now.Ticks;
        }

        public static void setOldScriptTimestamp()
        {
            OldScriptTimestamp = GetTimestamp() + 10;
        }

        public static bool scriptIsOld(long scriptTimestamp)
        {
            return (scriptTimestamp < OldScriptTimestamp) || (scriptTimestamp + ScriptTimeOut.Ticks < GetTimestamp());
        }


        public static void ExecuteCommand(string command, int LayerId)
        {
            var add = EngineGetById(LayerId, "");
            try
            {
                Javascript.Functions.Print("> " + command, LayerId);
                string evaluateResultString = string.Empty;// add.Evaluate(command).ToString();
                JsValue evaluateResult = add.Evaluate(command);
                if ((evaluateResult.IsArray() || evaluateResult.IsObject()) && evaluateResult.GetType().FullName != "Jint.Runtime.Interop.DelegateWrapper")
                {
                    evaluateResultString = evaluateResult.ToString().Replace("[]", "") + JsonConvert.SerializeObject(evaluateResult.ToObject());
                }
                else
                {
                    evaluateResultString = evaluateResult.ToString();
                    if (evaluateResult.IsString())
                    {
                        evaluateResultString = "\"" + evaluateResultString + "\"";
                    }

                }
                if (!string.IsNullOrEmpty(evaluateResultString) && evaluateResultString != "null")
                {
                    Javascript.Functions.Print("< " + evaluateResultString, LayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Javascript.Functions.PrintError(ex.Message);
            }
        }
    }
}
