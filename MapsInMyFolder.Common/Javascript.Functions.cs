using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
                    PrintError("The variable name is not defined!");
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
                    PrintError("The variable name is not defined!", LayerId);
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
                return null;
            }

            static public bool ClearVar(int LayerId, object variablename = null)
            {
                if (ReferenceEquals(null, variablename))
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
                }
            }
            #endregion

            public static void SetSelection(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, bool ZoomToNewLocation, int LayerId)
            {
                if (Layers.Current.class_id == LayerId)
                {
                    JavascriptInstance.Location = new Dictionary<string, double>(){
                        {"SE_Latitude",SE_Latitude },
                        {"SE_Longitude",SE_Longitude },
                        {"NO_Latitude",NO_Latitude },
                        {"NO_Longitude",NO_Longitude }
                    };
                    JavascriptInstance.ZoomToNewLocation = ZoomToNewLocation;
                }
                else
                {
                    PrintError("Unable to define the selection, the layer is not active.");
                }
            }

            public static Dictionary<string, Dictionary<string, double>> GetSelection()
            {
                return new Dictionary<string, Dictionary<string, double>>
                {
                    {
                        "SE", new Dictionary<string, double>() {
                            {"lat",Map.CurentSelection.SE_Latitude },
                            {"long",Map.CurentSelection.SE_Longitude }
                        }
                    },
                    {
                        "NO", new Dictionary<string, double>() {
                            {"lat",Map.CurentSelection.NO_Latitude },
                            {"long",Map.CurentSelection.NO_Longitude }
                        }
                    }
                };
            }

            public static Dictionary<string, int> CoordonneesToTile(object latitude, object longitude, object zoom)
            {
                int Intzoom = Convert.ToInt32(zoom);
                var TilesNumber = Collectif.CoordonneesToTile((double)latitude, (double)longitude, Intzoom);
                return new Dictionary<string, int>()
                {
                    { "x",  TilesNumber.X },
                    { "y",  TilesNumber.Y },
                    { "z",  Intzoom }
                };
            }
            public static Dictionary<string, double> TileToCoordonnees(object TileX, object TileY, object zoom)
            {
                int Intzoom = Convert.ToInt32(zoom);
                var LocationNumber = Collectif.TileToCoordonnees(Convert.ToInt32(TileX), Convert.ToInt32(TileY), Intzoom);
                return new Dictionary<string, double>()
                {
                    { "long",  LocationNumber.Latitude },
                    { "lat",  LocationNumber.Longitude },
                    { "z",  Intzoom}
                };
            }

            private static string ConvertJSObjectToString(object supposedString)
            {
                string returnString = supposedString?.ToString();

                if (returnString != null &&
                    (supposedString.GetType() == typeof(object) ||
                    supposedString.GetType() == typeof(object[]) ||
                    supposedString.GetType() == typeof(System.Dynamic.ExpandoObject) ||
                    supposedString.GetType() == typeof(Dictionary<string, string>) ||
                    supposedString.GetType() == typeof(Dictionary<string, object>))
                && (supposedString.GetType().FullName != "Jint.Runtime.Interop.DelegateWrapper"))
                {
                    returnString = JsonConvert.SerializeObject(supposedString);
                }

                return returnString;
            }

            public static void Print(object print, int LayerId = 0)
            {
                string printString = ConvertJSObjectToString(print);
                if (!string.IsNullOrEmpty(printString))
                {
                    if (LayerId == -2 && Tiles.AcceptJavascriptPrint)
                    {
                        JavascriptInstance.Logs = string.Concat(JavascriptInstance.Logs, "\n", printString);
                    }
                }
            }

            static public void PrintClear()
            {
                if (Tiles.AcceptJavascriptPrint)
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

                        void Dialog_Closed(object sender, EventArgs e)
                        {
                            frame.Continue = false;
                            dialog.Closed -= Dialog_Closed;
                        }

                        dialog.Closed += Dialog_Closed;
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
                        void Dialog_Closed(object sender, EventArgs e)
                        {
                            frame.Continue = false;
                            dialog.Closed -= Dialog_Closed;
                        }

                        dialog.Closed += Dialog_Closed;
                        dialog.ShowAsync();
                    }));
                    Dispatcher.PushFrame(frame);
                }
            }


            public static string SendNotification(int LayerId, object texte, object caption = null, object javascriptCallback = null, object notifId = null)
            {
                if (LayerId == -2 || LayerId != Layers.Current.class_id)
                {
                    PrintError("Unable to send a notification from the editor or if the layer is not active.");
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
                            ExecuteScript(Layers.GetLayerById(LayerId).class_tilecomputationscript, null, LayerId, javascriptCallback?.ToString());
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
                stringBuilder.AppendLine(" - sendNotification(\"message\", \"caption\", \"callback\", \"notificationId\") : Envoie une notification à l'écran. Un callback peut être attaché et appelé lors du clic sur celle-ci");
                stringBuilder.AppendLine(" - refreshMap() : Rafraîchit la carte à l'écran");
                stringBuilder.AppendLine(" - getStyle() : Obtient la valeur du style");

                Print(stringBuilder.ToString(), LayerId);
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
