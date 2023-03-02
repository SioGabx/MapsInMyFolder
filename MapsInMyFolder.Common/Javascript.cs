using Jint;
using Jint.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MapsInMyFolder.Commun
{
    public class Javascript
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

        private static Engine SetupEngine(int LayerId)
        {
            Engine add = new Engine(options =>
            {
                options.TimeoutInterval(TimeSpan.FromSeconds(15));
                options.MaxStatements(5000);
                CancellationTokenSource JsCancelToken = new CancellationTokenSource();
                if (!JsListCancelTocken.ContainsKey(LayerId))
                {
                    try
                    {
                        JsListCancelTocken.Add(LayerId, JsCancelToken);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error JsListCancelTocken " + ex.Message);
                    }
                }
                options.CancellationToken(JsCancelToken.Token);
            });

            Action<object> PrintAction = stringtext => Print(stringtext, LayerId);
            add.SetValue("print", PrintAction);

            Action<object> PrintClearAction = _ => PrintClear();
            add.SetValue("printClear", PrintClearAction);
            add.SetValue("cls", PrintClearAction);

            Action<object> helpAction = _ => Help(LayerId);
            add.SetValue("help", helpAction);

            Func<object, object, bool, object> setVarAction = (variable, value, isglobalvar) => SetVar(variable, value, isglobalvar, LayerId);
            add.SetValue("setVar", setVarAction); //setVar("variable1","valeur1")

            Func<object, object> getVarFunction = (variablename) => GetVar(variablename, LayerId);
            add.SetValue("getVar", getVarFunction); //getVar("variable1")

            Func<object, bool> clearVarFunction = (variablename) => ClearVar(LayerId, variablename);
            add.SetValue("clearVar", clearVarFunction);

            Func<object, object, object, object> getTileNumberAction = (latitude, longitude, zoom) => CoordonneesToTile(latitude, longitude, zoom);
            add.SetValue("getTileNumber", getTileNumberAction); //setVar("variable1","valeur1")

            Func<object, object, object, object> getLatLongAction = (TileX, TileY, zoom) => TileToCoordonnees(TileX, TileY, zoom);
            add.SetValue("getLatLong", getLatLongAction); //setVar("variable1","valeur1")

            Action<double, double, double, double, bool> setSelectionAction = (NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation) =>
            SetSelection(NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation, LayerId);
            add.SetValue("setSelection", setSelectionAction);

            Func<object> getSelectionAction = () => GetSelection();
            add.SetValue("getSelection", getSelectionAction);

            Action<object, object> alertAction = (texte, caption) => Alert(LayerId, texte, caption);
            add.SetValue("alert", alertAction);

            Func<object, object, object> inputboxAction = (texte, caption) => InputBox(LayerId, texte, caption);
            add.SetValue("inputbox", inputboxAction);

            Func<object, object, object, object, object> SendNotificationFunction = (texte, caption, callback, notifId) => SendNotification(LayerId, texte, caption, callback, notifId);
            add.SetValue("sendNotification", SendNotificationFunction);

            Func<object> refreshMap = () =>
            {
                JavascriptActionEvent?.Invoke(LayerId, JavascriptAction.refreshMap);
                return null;
            };
            add.SetValue("refreshMap", refreshMap);
            return add;
        }

        #region StoreVariable
        private static readonly Dictionary<int, Dictionary<string, object>> DictionnaryOfVariablesKeyLayerId = new Dictionary<int, Dictionary<string, object>>();
        static public object SetVar(object variablename, object value, bool isglobalvar = false, int LayerId = 0)
        {
            string variablenameString;
            if (variablename is null)
            {
                PrintError("Le nom de la variable n'est pas défini !");
                return null;
            }
            else
            {
                variablenameString = variablename.ToString();
            }

            if (isglobalvar)
            {
                LayerId = 0;
            }

            if (DictionnaryOfVariablesKeyLayerId.TryGetValue(LayerId, out Dictionary<string, object> VariableKeyAndValue))
            {
                VariableKeyAndValue[variablenameString] = value;
            }
            else
            {
                DictionnaryOfVariablesKeyLayerId.Add(LayerId, new Dictionary<string, object>() { { variablenameString, value } });
            }
            return value;
        }

        static public object GetVar(object variablename, int LayerId = 0)
        {
            string variablenameString;
            if (variablename is null)
            {
                PrintError("Le nom de la variable n'est pas défini !", LayerId);
                return null;
            }
            else
            {
                variablenameString = variablename.ToString();
            }

            //ID 0 is global scope
            foreach (int ID in new int[] { LayerId, 0 })
            {
                if (DictionnaryOfVariablesKeyLayerId.TryGetValue(ID, out Dictionary<string, object> VariableKeyAndValue))
                {
                    if (VariableKeyAndValue.ContainsKey(variablenameString))
                    {
                        return VariableKeyAndValue[variablenameString];
                    }
                }
            }
            PrintError("La variable " + variablenameString + " n'est pas défini", LayerId);
            return null;
        }

        static public bool ClearVar(int LayerId, object variablename = null)
        {
            if (object.ReferenceEquals(null, variablename))
            {
                if (DictionnaryOfVariablesKeyLayerId.ContainsKey(LayerId))
                {
                    DictionnaryOfVariablesKeyLayerId.Remove(LayerId);
                    return true;
                }
            }
            else
            {
                if (DictionnaryOfVariablesKeyLayerId.TryGetValue(LayerId, out Dictionary<string, object> VariableKeyAndValue))
                {
                    if (VariableKeyAndValue.ContainsKey(variablename.ToString()))
                    {
                        VariableKeyAndValue.Remove(variablename.ToString());
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DisposeVariablesOfLayer(int LayerId)
        {
            DictionnaryOfVariablesKeyLayerId.Remove(LayerId);
        }

        public static void DisposeVariable(string variablename, int LayerId)
        {
            if (DictionnaryOfVariablesKeyLayerId.TryGetValue(LayerId, out Dictionary<string, object> VariableKeyAndValue))
            {
                VariableKeyAndValue.Remove(variablename);
                Print("< Info : La variable à été disposée");
            }
            else
            {
                PrintError("La variable " + variablename + " n'existe pas");
            }
        }
        #endregion

        public static void SetSelection(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, bool ZoomToNewLocation, int LayerId)
        {
            if (Layers.Curent.class_id == LayerId)
            {
                JavascriptInstance.Location = new Dictionary<string, double>(){
                    {"SE_Latitude",SE_Latitude },
                    {"SE_Longitude",SE_Longitude },
                    {"NO_Latitude",NO_Latitude },
                    {"NO_Longitude",NO_Longitude }
                };
                JavascriptInstance.ZoomToNewLocation = ZoomToNewLocation;
            }
        }

        public static Dictionary<string, Dictionary<string, double>> GetSelection()
        {
            var ReturnDic = new Dictionary<string, Dictionary<string, double>>
            {
                {
                    "SE",
                    new Dictionary<string, double>() {
            {"lat",Map.CurentSelection.SE_Latitude },
            {"long",Map.CurentSelection.SE_Longitude }
            }
                },

                {
                    "NO",
                    new Dictionary<string, double>() {
            {"lat",Map.CurentSelection.NO_Latitude },
            {"long",Map.CurentSelection.NO_Longitude }
            }
                }
            };

            return ReturnDic;
        }

        public static Dictionary<string, int> CoordonneesToTile(object latitude, object longitude, object zoom)
        {
            int Intzoom = Convert.ToInt32(zoom);
            List<int> TilesNumber = Collectif.CoordonneesToTile((double)latitude, (double)longitude, Intzoom);
            Dictionary<string, int> returnTileNumber = new Dictionary<string, int>()
            {
                { "x",  TilesNumber[0] },
                { "y",  TilesNumber[1] },
                { "z",  Intzoom }
            };
            return returnTileNumber;
            //return returnTileNumber;
        }
        public static Dictionary<string, double> TileToCoordonnees(object TileX, object TileY, object zoom)
        {
            int Intzoom = Convert.ToInt32(zoom);
            List<double> LocationNumber = Collectif.TileToCoordonnees(Convert.ToInt32(TileX), Convert.ToInt32(TileY), Intzoom);
            Dictionary<string, double> returnLocationNumber = new Dictionary<string, double>()
            {
                { "long",  LocationNumber[0] },
                { "lat",  LocationNumber[1] },
                { "z",  Intzoom}
            };
            return returnLocationNumber;
        }

        private static string ConvertJSObjectToString(object supposedString)
        {

            string returnString = supposedString?.ToString();
            if (returnString != null && (supposedString.GetType() == typeof(System.Object) || supposedString.GetType() == typeof(System.Object[]) || supposedString.GetType() == typeof(System.Dynamic.ExpandoObject) || supposedString.GetType() == typeof(Dictionary<string, string>) || supposedString.GetType() == typeof(Dictionary<string, object>))
                && (supposedString.GetType().FullName != "Jint.Runtime.Interop.DelegateWrapper"))
            {
                Debug.WriteLine(supposedString.GetType().FullName);
                returnString = JsonConvert.SerializeObject(supposedString);
            }
            return returnString;
        }
        public static void Print(object print, int LayerId = 0)
        {
            string printString = ConvertJSObjectToString(print);
            if (!string.IsNullOrEmpty(printString))
            {
                if (LayerId == -2 && TileGeneratorSettings.AcceptJavascriptPrint)
                {
                    JavascriptInstance.Logs = String.Concat(JavascriptInstance.Logs, "\n", printString);
                }
            }
        }

        static public void PrintClear()
        {
            if (TileGeneratorSettings.AcceptJavascriptPrint)
            {
                JavascriptInstance.Logs = String.Empty;
            }
        }

        static readonly object JSLocker = new object();
        static public string InputBox(int LayerId, object texte, object caption = null)
        {
            lock (JSLocker)
            {
                //alert("pos");
                if (string.IsNullOrEmpty(caption?.ToString()))
                {
                    caption = Collectif.HTMLEntities(Layers.GetLayerById(LayerId).class_name, true) + " indique :";
                }
                TextBox TextBox;
                var frame = new DispatcherFrame();
                TextBox = Application.Current.Dispatcher.Invoke(new Func<TextBox>(() =>
                {
                    var (textBox, dialog) = Message.SetInputBoxDialog(texte, caption);
                    dialog.Closed += (_, __) =>
                    {
                        // stops the frame
                        frame.Continue = false;
                    };
                    dialog.ShowAsync();
                    return textBox;
                }));
                Dispatcher.PushFrame(frame);
                return Application.Current.Dispatcher.Invoke(new Func<string>(() => TextBox.Text));
            }
        }

        static public void Alert(int LayerId, object texte, object caption = null)
        {
            lock (JSLocker)
            {
                //alert("pos");
                if (string.IsNullOrEmpty(caption?.ToString()))
                {
                    caption = Collectif.HTMLEntities(Layers.GetLayerById(LayerId).class_name, true) + " indique :";
                }

                var frame = new DispatcherFrame();
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    var dialog = Message.SetContentDialog(texte, caption);
                    dialog.Closed += (_, __) =>
                    {
                        frame.Continue = false; // stops the frame
                    };
                    dialog.ShowAsync();
                }));
                Dispatcher.PushFrame(frame);
            }
        }


        static public string SendNotification(int LayerId, object texte, object caption = null, object javascriptCallback = null, object notifId = null)
        {
            if (LayerId == -2 || LayerId != Layers.Curent.class_id)
            {
                PrintError("Impossible d'envoyer une notification depuis l'editeur ou si le calque n'est pas courant");
                return null;
            }
            Notification notification = null;

            void SetupNotification()
            {
                Action callback = () => Javascript.ExecuteScript(Layers.GetLayerById(LayerId).class_tilecomputationscript, null, LayerId, javascriptCallback.ToString());

                notification = new NText(texte.ToString(), caption.ToString(), callback);
                if (notifId != null && !string.IsNullOrWhiteSpace(notifId.ToString()))
                {
                    notification.NotificationId = "LayerId_" + LayerId + "_" + notifId;
                }
                notification.Register();
            }
            if (System.Threading.Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                SetupNotification();
            }
          
            return notification?.NotificationId;
        }

        static public void Help(int LayerId)
        {
            Print(
                "\n\nAIDE CALCUL TUILE" + "\n" +
                "A chaque chargement de tuile, la function getTile est appelée avec des arguments\n" +
                "et dois retourner un objet contenant les remplacements à effectués.\n" +
                "Les arguments qui sont envoyé à la fonction de base sont les suivants : " + "\n" +
                " - X : Représente en WMTS le numero de la tuile X" + "\n" +
                " - Y : Représente en WMTS le numero de la tuile Y" + "\n" +
                " - Z : Représente le niveau de zoom" + "\n" +
                " - layerid : Représente l'ID du calque sélectionnée" + "\n" +
                "Exemple pour l'url \"https://tile.openstreetmap.org/{NiveauZoom}/{TuileX}/{TuileY}.png\":" + "\n" +
                "function getTile(args) {" + "\n" +
                "var returnvalue = new Object;" + "\n" +
                "returnvalue.TuileX = args.x;" + "\n" +
                "returnvalue.TuileY = args.y;" + "\n" +
                "returnvalue.NiveauZoom = args.z;" + "\n" +
                "return returnvalue;" + "\n" +
                "}" + "\n" +
                "\n" +
                "AIDE FUNCTIONS PERSONALISÉE" + "\n" +
                "print(string) : Affiche un message dans la console" + "\n" +
                "alert(string) : Affiche une boite de dialogue" + "\n" +
                "printClear(string) : Efface la console" + "\n" +
                "help() : Affiche cette aide" + "\n" +
                "setVar(\"nom_variable\",\"valeur\") : Defini la valeur de la variable. Cette variable est conservé durant l'entiéreté de l'execution de l'application" + "\n" +
                "getVar(\"nom_variable\") : Obtiens la valeur de la variable." + "\n" +
                "getTileNumber(latitude, longitude, zoom) : Converti les coordonnées en tiles" + "\n" +
                "getLatLong(TileX, TileY, zoom) : Converti les numero de tiles en coordonnées" + "\n" +
                "", LayerId);

        }

        static public void PrintError(string print, int LayerId = -2)
        {
            string errorType;
            if (print.EndsWith(" is not defined"))
            {
                errorType = "Uncaught ReferenceError";
            }
            else if (print.EndsWith("Unexpected token ILLEGAL"))
            {
                errorType = "Uncaught SyntaxError";
            }
            else
            {
                errorType = "Uncaught Error";
            }
            Print("< " + errorType + " : " + print, LayerId);
        }

        #region engines
        private static readonly Dictionary<int, CancellationTokenSource> JsListCancelTocken = new Dictionary<int, CancellationTokenSource>();
        private static readonly Dictionary<int, Engine> ListOfEngines = new Dictionary<int, Engine>();

        public static void EngineStopAll()
        {
            foreach (KeyValuePair<int, CancellationTokenSource> tockensource in JsListCancelTocken)
            {
                tockensource.Value.Cancel();
            }
        }
        public static void EngineStopById(int LayerId)
        {
            if (JsListCancelTocken.TryGetValue(LayerId, out CancellationTokenSource tockensource))
            {
                tockensource.Cancel();
            }
        }

        static readonly object locker = new object();
        public static Engine EngineGetById(int LayerId, string script)
        {
            if (ListOfEngines.TryGetValue(LayerId, out Engine engine))
            {
                return engine;
            }
            else
            {
                if (ListOfEngines.Count > 200)
                {
                    //todo : add setting for that
                    EngineClearList();
                }
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
                    EngineDeleteById(LayerId);
                    Debug.WriteLine(ex.Message);
                    PrintError(ex.Message);
                }
                return null;
            }
        }

        public static void EngineDeleteById(int LayerId)
        {
            ListOfEngines.Remove(LayerId);
            JsListCancelTocken.Remove(LayerId);
        }
        public static void EngineUpdate(Engine engine, int LayerId)
        {
            ListOfEngines[LayerId] = engine;
        }

        public static void EngineClearList()
        {
            ListOfEngines.Clear();
            JsListCancelTocken.Clear();
        }
        #endregion


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

            lock (locker)
            {
                Engine add = EngineGetById(LayerId, script);
                if (add is null)
                {
                    return false;
                }
                JsValue evaluateValue = add.Evaluate("typeof " + functionName + " === 'function'");

                return evaluateValue.AsBoolean();
            }
        }

        public static Jint.Native.JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, Collectif.GetUrl.InvokeFunction InvokeFunction)
        {
            return ExecuteScript(script, arguments, LayerId, InvokeFunction.ToString());
        }
        public static Jint.Native.JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId, string InvokeFunctionString)
        {
            lock (locker)
            {
                Engine add = EngineGetById(LayerId, script);
                if (add is null)
                {
                    return null;
                }
                Jint.Native.JsValue jsValue = null;
                try
                {
                    jsValue = add.Invoke(InvokeFunctionString, arguments);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (ex.Message == "Can only invoke functions")
                    {
                        //PrintError("No main function fund. Use \"function getT(args) {}\"");
                        PrintError("La fontion " + InvokeFunctionString + " n'as pas été trouvé dans le script. Faite help() pour obtenir de l'aide sur cette commande.");
                    }
                    else
                    {
                        PrintError(ex.Message);
                    }
                }
                EngineUpdate(add, LayerId);
                return jsValue;
            }
        }

        public static void ExecuteCommand(string command, int LayerId)
        {
            var add = EngineGetById(LayerId, "");
            try
            {
                Print("> " + command, LayerId);
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
                    Print("< " + evaluateResultString, LayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                PrintError(ex.Message);
            }
        }
    }
}
