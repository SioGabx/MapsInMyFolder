using Jint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public class Javascript
    {
        #region logs
        //Register logger to the CustomOrEditPage
        public static Javascript JavascriptInstance = new Javascript();
        private string _logs;
        public class LogsEventArgs : EventArgs
        {
            public string Logs { get; set; }
        }
        public delegate void LogsChangedHandler(object source, LogsEventArgs e);
        public event EventHandler<LogsEventArgs> LogsChanged;
        protected virtual void OnLogsChanged()
        {
            LogsChanged?.Invoke(this, new LogsEventArgs { Logs = _logs });
        }
        public string Logs
        {
            get { return _logs; }
            set
            {
                if (Commun.TileGeneratorSettings.AcceptJavascriptPrint)
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
            var add = new Engine(options =>
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
                else
                {
                    Debug.WriteLine("JsListCancelTocken - An item with the same key has already been added. Key: " + LayerId);
                }
                options.CancellationToken(JsCancelToken.Token);
            });
            Action<object> PrintAction = stringtext => Javascript.Print(String.Concat(">", stringtext), LayerId);
            add.SetValue("print", PrintAction);
            Action<object> AlertAction = stringtext => Javascript.Alert(stringtext, LayerId);
            add.SetValue("alert", AlertAction);
            Action<object> PrintClearAction = _ => Javascript.PrintClear();
            add.SetValue("printClear", PrintClearAction);
            Action<object> helpAction = _ => Javascript.Help(LayerId);
            add.SetValue("help", helpAction);
            Action<object, object> setVarAction = (variable, value) => Javascript.SetVar(variable, value, LayerId);
            add.SetValue("setVar", setVarAction); //setVar("variable1","valeur1")
            Func<object, object> getVarFunction = (variablename) => GetVar(variablename, LayerId);
            add.SetValue("getVar", getVarFunction); //getVar("variable1")

            Func<object, object, object, object> getTileNumberAction = (latitude, longitude, zoom) => CoordonneesToTile(latitude, longitude, zoom);
            add.SetValue("getTileNumber", getTileNumberAction); //setVar("variable1","valeur1")

            Func<object, object, object, object> getLatLongAction = (TileX, TileY, zoom) => TileToCoordonnees(TileX, TileY, zoom);
            add.SetValue("getLatLong", getLatLongAction); //setVar("variable1","valeur1")

            Action<double, double, double, double, bool> setSelectionAction = (NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation) =>
            SetSelection(NO_Latitude, NO_Longitude, SE_Latitude, SE_Longitude, ZoomToNewLocation, LayerId);
            add.SetValue("setSelection", setSelectionAction);

            Func<object> getSelectionAction = () => GetSelection();
            add.SetValue("getSelection", getSelectionAction);
            return add;
        }

        #region StoreVariable
        private static readonly Dictionary<int, Dictionary<string, object>> DictionnaryOfVariablesKeyLayerId = new Dictionary<int, Dictionary<string, object>>();
        static public void SetVar(object variablename, object value, int LayerId = 0)
        {
            string variablenameString;
            if (variablename is null)
            {
                PrintError("Le nom de la variable n'est pas défini !");
                return;
            }
            else
            {
                variablenameString = variablename.ToString();
            }

            if (DictionnaryOfVariablesKeyLayerId.TryGetValue(LayerId, out Dictionary<string, object> VariableKeyAndValue))
            {
                //if (VariableKeyAndValue.ContainsKey(variablenameString))
                //{
                //    VariableKeyAndValue[variablenameString] = value;
                //}
                //else
                //{
                //    VariableKeyAndValue.Add(variablenameString, value);
                //} 
                VariableKeyAndValue[variablenameString] = value;
            }
            else
            {
                DictionnaryOfVariablesKeyLayerId.Add(LayerId, new Dictionary<string, object>() { { variablenameString, value } });
            }
        }

        //todo add get / set parameters + add option to print error from DownloadByteUrl
        static public object GetVar(object variablename, int LayerId = 0)
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
            if (DictionnaryOfVariablesKeyLayerId.TryGetValue(LayerId, out Dictionary<string, object> VariableKeyAndValue))
            {
                if (VariableKeyAndValue.ContainsKey(variablenameString))
                {
                    return VariableKeyAndValue[variablenameString];
                }
            }
            PrintError("La variable " + variablenameString + " n'est pas défini !");
            return null;
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
                Print("Info : La variable à été disposée");
            }
            else
            {
                PrintError("La variable " + variablename + " n'existe pas");
            }
        }
        #endregion

        public static void SetSelection(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, bool ZoomToNewLocation, int LayerId)
        {
            if (Curent.Layer.class_id == LayerId)
            {
                Javascript.JavascriptInstance.Location = new Dictionary<string, double>(){
                    {"SE_Latitude",SE_Latitude },
                    {"SE_Longitude",SE_Longitude },
                    {"NO_Latitude",NO_Latitude },
                    {"NO_Longitude",NO_Longitude }
                };
                Javascript.JavascriptInstance.ZoomToNewLocation = ZoomToNewLocation;
            }
        }

        public static Dictionary<string, Dictionary<string, double>> GetSelection()
        {
            var ReturnDic = new Dictionary<string, Dictionary<string, double>>
            {
                {
                    "SE",
                    new Dictionary<string, double>() {
            {"lat",Curent.Selection.SE_Latitude },
            {"long",Curent.Selection.SE_Longitude }
            }
                },

                {
                    "NO",
                    new Dictionary<string, double>() {
            {"lat",Curent.Selection.NO_Latitude },
            {"long",Curent.Selection.NO_Longitude }
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

        public static void Print(string print, int LayerId = 0)
        {
            if (!string.IsNullOrEmpty(print))
            {
                DebugMode.WriteLine("Javascript print : " + LayerId + " " + print);
                if (LayerId == -2 && Commun.TileGeneratorSettings.AcceptJavascriptPrint)
                {
                    Javascript.JavascriptInstance.Logs = String.Concat(Javascript.JavascriptInstance.Logs, "\n", print);
                    return;
                }
            }
        }

        static public void PrintClear()
        {
            DebugMode.WriteLine("JSClear console");
            if (Commun.TileGeneratorSettings.AcceptJavascriptPrint)
            {
                Javascript.JavascriptInstance.Logs = String.Empty;
            }
        }

        static public void Help(int LayerId)
        {
            Print(
                "\n\nAIDE CALCUL TUILE" + "\n" +
                "A chaque chargement de tuile, la function main est appelée avec des arguments\n" +
                "et dois retourner un objet contenant les remplacements à effectués.\n" +
                "Les arguments qui sont envoyé à la fonction de base sont les suivants : " + "\n" +
                " - X : Représente en WMTS le numero de la tuile X" + "\n" +
                " - Y : Représente en WMTS le numero de la tuile Y" + "\n" +
                " - Z : Représente le niveau de zoom" + "\n" +
                " - layerid : Représente l'ID du calque sélectionnée" + "\n" +
                "Exemple pour l'url \"https://tile.openstreetmap.org/{NiveauZoom}/{TuileX}/{TuileY}.png\":" + "\n" +
                "function main(args) {" + "\n" +
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
                "printClear(string) : Efface la console" +
                "help() : Affiche cette aide" +
                "setVar(\"nom_variable\",\"valeur\") : Defini la valeur de la variable. Cette variable est conservé durant l'entiéreté de l'execution de l'application" +
                "getVar(\"nom_variable\") : Obtiens la valeur de la variable." +
                "getTileNumber(latitude, longitude, zoom) : Converti les coordonnées en tiles" +
                "getLatLong(TileX, TileY, zoom) : Converti les numero de tiles en coordonnées" +
                "", LayerId);

            /*//
            var lat = 48.1995382391003;
var long = 6.42831455369568;

var tiles = getTileNumber(lat, long, 18)
print("x : " + tiles.x + " / y : " + tiles.y)

var locations = getLatLong(tiles.x, tiles.y, 18)
print("lat : " + locations.lat + " / long : " + locations.long)
             
             
             */
            Debug.WriteLine("help triggered" + LayerId);
        }

        static public void PrintError(string print, int LayerId = -2)
        {
            Print("Error : " + print, LayerId);
        }

        static void Alert(object Message, int LayerId)
        {
            if (Curent.Layer.class_id == LayerId)
            {
                string text = Message.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    MessageBox.Show(text, "MapsInMyFolder");
                }
            }
        }

        #region engines
        private static readonly Dictionary<int, CancellationTokenSource> JsListCancelTocken = new Dictionary<int, CancellationTokenSource>();
        private static readonly Dictionary<int, Engine> ListOfEngines = new Dictionary<int, Engine>();

        public static void EngineStopAll()
        {
            foreach (KeyValuePair<int, CancellationTokenSource> tockensource in JsListCancelTocken)
            {
                DebugMode.WriteLine("Canceled JSengine " + tockensource.Key);
                tockensource.Value.Cancel();
            }
        }
        public static void EngineStopById(int LayerId)
        {
            if (JsListCancelTocken.TryGetValue(LayerId, out CancellationTokenSource tockensource))
            {
                DebugMode.WriteLine("Canceled JSengine " + LayerId);
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
                DebugMode.WriteLine("CreateEngine " + LayerId);
                if (ListOfEngines.Count > 100)
                {
                    DebugMode.WriteLine("ClearEngine >100");
                    EngineClearList();
                }
                Engine add = SetupEngine(LayerId);
                try
                {
                    add.Execute(script);
                    return add;
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
            DebugMode.WriteLine("RemoveEngine " + LayerId);
            ListOfEngines.Remove(LayerId);
            JsListCancelTocken.Remove(LayerId);
        }
        public static void EngineUpdate(Engine engine, int LayerId)
        {
            ListOfEngines[LayerId] = engine;
        }

        public static void EngineClearList()
        {
            DebugMode.WriteLine("ClearEngine");
            ListOfEngines.Clear();
            JsListCancelTocken.Clear();
        }
        #endregion
        public static Jint.Native.JsValue ExecuteScript(string script, Dictionary<string, object> arguments, int LayerId)
        {
            DebugMode.WriteLine("DEBUG JS : LockLayerId" + LayerId);
            lock (locker)
            {
                DebugMode.WriteLine("DEBUG JS : GetEngineLayerId" + LayerId);
                Engine add = EngineGetById(LayerId, script);
                DebugMode.WriteLine("DEBUG JS : Engine gotLayerId" + LayerId);

                if (add is null)
                {
                    DebugMode.WriteLine("DEBUG JS : engine nullLayerId" + LayerId);
                    return null;
                }
                Jint.Native.JsValue jsValue = null;
                try
                {
                    DebugMode.WriteLine("DEBUG JS : call mainLayerId" + LayerId);
                    jsValue = add.Invoke("main", arguments);
                    //add.SetValue("args", arguments);
                    //jsValue = add.Evaluate("main(args)");
                    DebugMode.WriteLine("DEBUG JS : end call mainLayerId" + LayerId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (ex.Message == "Can only invoke functions")
                    {
                        PrintError("No main function fund. Use \"function main(args) {}\"");
                    }
                    else
                    {
                        PrintError(ex.Message);
                    }
                }
                DebugMode.WriteLine("DEBUG JS : update engineLayerId" + LayerId);
                EngineUpdate(add, LayerId);
                DebugMode.WriteLine("DEBUG JS : returnLayerId" + LayerId);

                return jsValue;
            }
        }

        public static void ExecuteCommand(string command, int LayerId)
        {
            Debug.WriteLine("ExecuteCommand LayerId" + LayerId);
            var add = EngineGetById(LayerId, "");
            try
            {
                add.Evaluate(command);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                PrintError(ex.Message);
            }
        }
    }
}
