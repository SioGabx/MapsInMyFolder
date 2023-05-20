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
        public static class Functions
        {
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
                if (Layers.Current.class_id == LayerId)
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
                var TilesNumber = Collectif.CoordonneesToTile((double)latitude, (double)longitude, Intzoom);
                Dictionary<string, int> returnTileNumber = new Dictionary<string, int>()
            {
                { "x",  TilesNumber.X },
                { "y",  TilesNumber.Y },
                { "z",  Intzoom }
            };
                return returnTileNumber;
                //return returnTileNumber;
            }
            public static Dictionary<string, double> TileToCoordonnees(object TileX, object TileY, object zoom)
            {
                int Intzoom = Convert.ToInt32(zoom);
                var LocationNumber = Collectif.TileToCoordonnees(Convert.ToInt32(TileX), Convert.ToInt32(TileY), Intzoom);
                Dictionary<string, double> returnLocationNumber = new Dictionary<string, double>()
            {
                { "long",  LocationNumber.Latitude },
                { "lat",  LocationNumber.Longitude },
                { "z",  Intzoom}
            };
                return returnLocationNumber;
            }

            private static string ConvertJSObjectToString(object supposedString)
            {
                string returnString = supposedString?.ToString();

                if (returnString != null &&
                    (supposedString is object || supposedString is object[] ||
                    supposedString is System.Dynamic.ExpandoObject || supposedString is Dictionary<string, string> ||
                    supposedString is Dictionary<string, object>) &&
                    supposedString.GetType().FullName != "Jint.Runtime.Interop.DelegateWrapper")
                {
                    //Debug.WriteLine(supposedString.GetType().FullName);
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
                        Javascript.JavascriptInstance.Logs = String.Concat(Javascript.JavascriptInstance.Logs, "\n", printString);
                    }
                }
            }

            static public void PrintClear()
            {
                if (TileGeneratorSettings.AcceptJavascriptPrint)
                {
                    Javascript.JavascriptInstance.Logs = String.Empty;
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


            public static string SendNotification(int LayerId, object texte, object caption = null, object javascriptCallback = null, object notifId = null)
            {
                if (LayerId == -2 || LayerId != Layers.Current.class_id)
                {
                    PrintError("Impossible d'envoyer une notification depuis l'editeur ou si le calque n'est pas courant");
                    return null;
                }
                Notification notification = null;

                void SetupNotification()
                {
                    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    {
                        string NotificationId = "LayerId_" + LayerId + "_" + notifId ?? "single";
                        Action callback = () =>
                        {
                            Debug.WriteLine("CallBack");
                            Javascript.ExecuteScript(Layers.GetLayerById(LayerId).class_tilecomputationscript, null, LayerId, javascriptCallback?.ToString());
                        };

                        notification = new NText(texte.ToString(), caption?.ToString(), "MainPage", callback);
                        notification.NotificationId = NotificationId;
                        notification.Register();
                    }
                }

                Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() => SetupNotification());
                });

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

        }
    }
}
