using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public partial class Javascript
    {
        public enum InvokeFunction { getTile, getPreview, getPreviewFallback, selectionChanged }
        public enum JavascriptAction { refreshMap, clearCache }
        public static event EventHandler<JavascriptAction> JavascriptActionEvent;

        #region logs
        //Register logger to the CustomOrEditPage
        public static Javascript instance = new Javascript();
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
                if (Tiles.AcceptJavascriptPrint)
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
        public static bool IsWaitingUserAction;
        public static readonly TimeSpan ScriptTimeOut = new TimeSpan(0, 0, 4);

        private static Engine SetupEngine(int LayerId)
        {
            CancellationTokenSource JsCancelToken = new CancellationTokenSource();

            if (JsListCancelTocken.TryGetValue(LayerId, out CancellationTokenSource cancelTockenToDispose))
            {
                cancelTockenToDispose.Dispose();
                JsListCancelTocken.Remove(LayerId);
            }

            JsListCancelTocken.Add(LayerId, JsCancelToken);

            Engine add = new Engine(options =>
            {
                options.TimeoutInterval(new TimeSpan(0, 1, 0));
                options.MaxStatements(5000);
                options.CancellationToken(JsCancelToken.Token);
            });

            return SetupFunctions(add, LayerId);
        }

        public static Engine SetupFunctions(Engine engine, int LayerId)
        {
            engine.SetValue("print", (Action<object>)(stringtext => Functions.Print(stringtext, LayerId)));
            engine.SetValue("printClear", (Action<object>)(_ => Functions.PrintClear()));
            engine.SetValue("cls", (Action<object>)(_ => Functions.PrintClear()));
            engine.SetValue("help", (Action<object>)(_ => Functions.Help(LayerId)));
            engine.SetValue("setVar", (Func<object, object, bool, object>)((variable, value, isglobalvar) => Functions.SetVar(variable, value, isglobalvar, LayerId)));
            engine.SetValue("getVar", (Func<object, object>)(variablename => Functions.GetVar(variablename, LayerId)));
            engine.SetValue("clearVar", (Func<object, bool>)(variablename => Functions.ClearVar(LayerId, variablename)));
            engine.SetValue("getTileNumber", (Func<object, object, object, object>)((latitude, longitude, zoom) => Functions.CoordonneesToTile(latitude, longitude, zoom)));
            engine.SetValue("getLatLong", (Func<object, object, object, object>)((TileX, TileY, zoom) => Functions.TileToCoordonnees(TileX, TileY, zoom)));
            engine.SetValue("setSelection", (Action<double, double, double, double, bool>)((NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation) =>
                Functions.SetSelection(NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation, LayerId)));
            engine.SetValue("getSelection", (Func<object>)(() => Functions.GetSelection()));
            engine.SetValue("getView", (Func<object>)(() => Functions.GetView()));
            engine.SetValue("alert", (Action<object, object>)((texte, caption) => Functions.Alert(LayerId, texte, caption)));
            engine.SetValue("inputbox", (Func<object, object, object>)((texte, caption) => Functions.InputBox(LayerId, texte, caption)));
            engine.SetValue("notification", (Func<object, object, object, object, object, object>)((texte, caption, callback, notifId, doreplace) =>
                Functions.SendNotification(LayerId, texte, caption, callback, notifId, doreplace)));
            engine.SetValue("refreshMap", (Func<object>)(() =>
            {
                JavascriptActionEvent?.Invoke(LayerId, JavascriptAction.refreshMap);
                return null;
            }));
            engine.SetValue("clearCache", (Func<object>)(() => {
                JavascriptActionEvent?.Invoke(LayerId, JavascriptAction.clearCache);
                return null;
            }));

            engine.SetValue("getStyle", (Func<object>)(() => Tiles.Loader.GetStyle(LayerId)));
            engine.SetValue("transformLocation", (Func<object, object, object, object, object>)((OriginWkt, TargetWkt, ProjX, ProjY) =>
         Functions.TransformLocation(OriginWkt, TargetWkt, ProjX, ProjY)));
            engine.SetValue("transformLocationFromWGS84", (Func<object, object, object, object>)((TargetWkt, ProjX, ProjY) =>
         Functions.TransformLocationFromWGS84(TargetWkt, ProjX, ProjY)));

            engine.SetValue("btoa", (Func<object, object>)(stringToEncode => Functions.Base64Encode(stringToEncode, LayerId)));
            engine.SetValue("atob", (Func<object, object>)(stringToDecode => Functions.Base64Decode(stringToDecode, LayerId)));


            return engine;
        }

        public static string getHelp()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("AIDE");
            stringBuilder.AppendLine("À chaque chargement de tuile, la fonction getTile est appelée avec des arguments");
            stringBuilder.AppendLine("et doit retourner un objet contenant les remplacements à effectuer.");
            stringBuilder.AppendLine("La documentation du logiciel est disponible à cette adresse : ");
            stringBuilder.AppendLine("https://github.com/SioGabx/MapsInMyFolder");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("FONCTIONS");
            stringBuilder.AppendLine(" - print(string) : Affiche un message dans la console");
            stringBuilder.AppendLine(" - printClear() : Efface la console");
            stringBuilder.AppendLine(" - cls() : Efface la console");
            stringBuilder.AppendLine(" - help() : Affiche cette aide");
            stringBuilder.AppendLine(" - setVar(\"nom_variable\",\"valeur\") : Définit la valeur de la variable. Cette variable est conservée durant l'intégralité de l'exécution de l'application");
            stringBuilder.AppendLine(" - getVar(\"nom_variable\") : Obtient la valeur de la variable.");
            stringBuilder.AppendLine(" - clearVar(\"nom_variable\") : Supprime la variable.");
            stringBuilder.AppendLine(" - getTileNumber(latitude, longitude, zoom) : Convertit les coordonnées en tiles");
            stringBuilder.AppendLine(" - getLatLong(TileX, TileY, zoom) : Convertit les numéros de tiles en coordonnées");
            stringBuilder.AppendLine(" - setSelection(top_latitude, top_longitude, bot_latitude, bot_longitude, zoomToBound) : Définit les coordonnées de la sélection courante");
            stringBuilder.AppendLine(" - getSelection() : Obtient les coordonnées de la sélection courante");
            stringBuilder.AppendLine(" - alert(\"message\", \"caption\") : Affiche un message à l'écran");
            stringBuilder.AppendLine(" - inputbox(\"message\", \"caption\") : Demande une saisie à l'utilisateur");
            stringBuilder.AppendLine(" - notification(\"message\", \"caption\", \"callback\", \"notificationId\", \"replaceOld\") : Envoie une notification à l'écran. Un callback peut être attaché et appelé lors du clic sur celle-ci");
            stringBuilder.AppendLine(" - refreshMap() : Rafraîchit la carte à l'écran");
            stringBuilder.AppendLine(" - clearCache() : Nettoie le cache du calque");
            stringBuilder.AppendLine(" - getStyle() : Obtient la valeur du style");
            stringBuilder.AppendLine(" - transformLocation(OriginWkt, TargetWkt, projX, projY) : Convertir la position X, Y d'un système de coordonnées vers un autre système (utilise Well Known Text definition)");
            stringBuilder.AppendLine(" - transformLocationFromWGS84(TargetWkt, Latitude, Longitude) : Convertir les coordonnées vers un autre système de coordonnées (utilise Well Known Text definition)");

           return stringBuilder.ToString();
        }


        #region engines
        private static readonly Dictionary<int, CancellationTokenSource> JsListCancelTocken = new Dictionary<int, CancellationTokenSource>();
        private static readonly Dictionary<int, Engine> ListOfEngines = new Dictionary<int, Engine>();

        public static void EngineStopAll()
        {
            SetOldScriptTimestamp();
            foreach (KeyValuePair<int, CancellationTokenSource> tockensource in JsListCancelTocken)
            {
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
                    if (CheckIfFunctionExist(add, nameof(Javascript.InvokeFunction.getTile)))
                    {
                        return add;
                    }
                    else
                    {
                        return add.Execute(Settings.tileloader_default_script);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Functions.PrintError(ex.Message);
                }
            }
            return null;
        }

        public static void EngineDeleteById(int LayerId)
        {
            if (ListOfEngines.TryGetValue(LayerId, out Engine engineToDispose))
            {
                ListOfEngines.Remove(LayerId);
                engineToDispose.Dispose();
            }

            if (JsListCancelTocken.TryGetValue(LayerId, out CancellationTokenSource cancelTockenToDispose))
            {
                JsListCancelTocken.Remove(LayerId);
                cancelTockenToDispose.Dispose();
            }
        }

        public static void EngineUpdate(Engine engine, int LayerId)
        {
            ListOfEngines[LayerId] = engine;
        }

        public static void EngineClearList()
        {
            SetOldScriptTimestamp();
            ListOfEngines.Values.DisposeItems();
            ListOfEngines.Clear();
            JsListCancelTocken.Values.DisposeItems();
            JsListCancelTocken.Clear();
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
        private static string ConvertJSObjectToString(object supposedString)
        {
            string returnString = supposedString?.ToString();

            if (returnString != null &&
                (supposedString.GetType() == typeof(object) ||
                supposedString.GetType() == typeof(object[]) ||
                supposedString.GetType() == typeof(System.Dynamic.ExpandoObject) ||
                supposedString.GetType() == typeof(Dictionary<string, string>) ||
                supposedString.GetType() == typeof(Dictionary<string, int>) ||
                supposedString.GetType() == typeof(Dictionary<string, double>) ||
                supposedString.GetType() == typeof(Dictionary<string, object>))
            && (supposedString.GetType().FullName != "Jint.Runtime.Interop.DelegateWrapper"))
            {
                returnString = JsonConvert.SerializeObject(supposedString);
            }

            return returnString;
        }

        public static bool CheckIfFunctionExist(int LayerId, string functionName, string script = null)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                return false;
            }
            if (string.IsNullOrEmpty(script))
            {
                script = Layers.GetLayerById(LayerId)?.class_script;
            }
            if (string.IsNullOrWhiteSpace(script) || !script.Contains(functionName))
            {
                return false;
            }

            Engine add = EngineGetById(LayerId, script);

            if (add is null || string.IsNullOrEmpty(functionName))
            {
                return false;
            }
            try
            {
                JsValue evaluateValue = add?.Evaluate($"typeof {functionName} === 'function'");
                return evaluateValue?.AsBoolean() ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
        public static JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, Javascript.InvokeFunction InvokeFunction)
        {
            return ExecuteScript(script, arguments, LayerId, InvokeFunction.ToString());
        }

        public static JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, string InvokeFunctionString)
        {
            long ScriptTimestamp = GetTimestamp();
            try
            {
                Monitor.Enter(ExecuteScriptLocker);

                if (ScriptIsOld(ScriptTimestamp))
                {
                    Functions.PrintError("Execution of the function has been canceled because its start date is too old and/or has been revoked.");
                    return null;
                }
                bool executedSuccessfully = false;

                var task = Task.Run(async () =>
                {
                    await Task.Delay(10000); // Délai de 10 secondes

                    if (!executedSuccessfully && !IsWaitingUserAction)
                    {
                        Functions.PrintError("The execution of the function has been canceled as it took too long to respond.");
                        Monitor.Exit(ExecuteScriptLocker);
                        return;
                    }
                    //The task has been successfully executed!
                });

                Engine add = EngineGetById(LayerId, script);

                if (add is null)
                {
                    return null;
                }
                JsValue jsValue = null;

                try
                {
                    jsValue = add?.Invoke(InvokeFunctionString, arguments);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ExecuteScript " + ex.Message);
                    if (ex.Message == "Can only invoke functions")
                    {
                        Functions.PrintError(InvokeFunctionString + "=> " + ex.Message);
                    }
                    else
                    {
                        Functions.PrintError(ex.Message);
                    }
                    return null;
                }
                finally
                {
                    executedSuccessfully = true;
                    EngineUpdate(add, LayerId);
                }

                return jsValue;
            }
            finally
            {
                if (Monitor.IsEntered(ExecuteScriptLocker))
                {
                    Monitor.Exit(ExecuteScriptLocker);
                }
            }
        }



        public static long GetTimestamp()
        {
            return DateTime.Now.Ticks;
        }

        public static void SetOldScriptTimestamp()
        {
            OldScriptTimestamp = GetTimestamp() + 10;
        }

        public static bool ScriptIsOld(long scriptTimestamp)
        {
            return (scriptTimestamp < OldScriptTimestamp) || (scriptTimestamp + ScriptTimeOut.Ticks < GetTimestamp());
        }

        public static void ExecuteCommand(string command, int LayerId)
        {
            var add = EngineGetById(LayerId, "");
            try
            {
                Functions.Print("> " + command, LayerId);
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
                    Functions.Print("< " + evaluateResultString, LayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Functions.PrintError(ex.Message);
            }
        }
    }
}
