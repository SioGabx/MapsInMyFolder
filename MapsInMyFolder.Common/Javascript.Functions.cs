using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml.Serialization;

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
                if (variablename is null)
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
                    instance.Location = new Dictionary<string, double>(){
                        {"SE_Latitude",SE_Latitude },
                        {"SE_Longitude",SE_Longitude },
                        {"NO_Latitude",NO_Latitude },
                        {"NO_Longitude",NO_Longitude }
                    };
                    instance.ZoomToNewLocation = ZoomToNewLocation;
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

            public static Dictionary<string, int> CoordonneesToTile(object objLatitude, object objLongitude, object objZoom)
            {
                if (!(int.TryParse(objZoom?.ToString(), out int Zoom)))
                {
                    PrintError($"Unable to convert zoom \"{objZoom}\" to integer");
                    return null;
                }
                if (!(double.TryParse(objLatitude?.ToString(), out double latitude)))
                {
                    PrintError($"Unable to convert latitude \"{objLatitude}\" to double");
                    return null;
                }
                if (!(double.TryParse(objLongitude?.ToString(), out double longitude)))
                {
                    PrintError($"Unable to convert longitude \"{objLongitude}\" to double");
                    return null;
                }

                var (X, Y) = Collectif.CoordonneesToTile(latitude, longitude, Zoom);
                return new Dictionary<string, int>()
                {
                    { "x",  X },
                    { "y",  Y },
                    { "z",  Zoom }
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

            public static double[] TransformLocationFromWGS84(object TargetWkt, object oLatitude, object oLongitude)
            {
                if (!(double.TryParse(oLatitude?.ToString(), out double latitude)))
                {
                    PrintError($"Unable to convert latitude \"{oLatitude}\" to double");
                    return null;
                }
                if (!(double.TryParse(oLongitude?.ToString(), out double longitude)))
                {
                    PrintError($"Unable to convert longitude \"{oLongitude}\" to double");
                    return null;
                }
                if (string.IsNullOrEmpty(TargetWkt?.ToString()))
                {
                    PrintError($"TargetWkt is not defined");
                    return null;
                }
                return TileLocationCalculator.TransformLocationFromWGS84(TargetWkt.ToString(), latitude, longitude);
            }

            public static double[] TransformLocation(object OriginWkt, object TargetWkt, object oProjX, object oProjY)
            {
                if (!(double.TryParse(oProjX?.ToString(), out double latitude)))
                {
                    PrintError($"Unable to convert latitude \"{oProjX}\" to double");
                    return null;
                }
                if (!(double.TryParse(oProjY?.ToString(), out double longitude)))
                {
                    PrintError($"Unable to convert longitude \"{oProjY}\" to double");
                    return null;
                }
                if (string.IsNullOrEmpty(OriginWkt?.ToString()))
                {
                    PrintError($"OriginWkt is not defined");
                    return null;
                }
                if (string.IsNullOrEmpty(TargetWkt?.ToString()))
                {
                    PrintError($"TargetWkt is not defined");
                    return null;
                }
                return TileLocationCalculator.TransformLocation(OriginWkt.ToString(), TargetWkt.ToString(), latitude, longitude);
            }
   
            public static void Print(object print, int LayerId = 0)
            {
                string printString = ConvertJSObjectToString(print);
                if (!string.IsNullOrEmpty(printString))
                {
                    if (LayerId == -2 && Tiles.AcceptJavascriptPrint)
                    {
                        instance.Logs = string.Concat(instance.Logs, "\n", printString);
                    }
                }
            }

            static public void PrintClear()
            {
                if (Tiles.AcceptJavascriptPrint)
                {
                    instance.Logs = String.Empty;
                }
            }

            static public string InputBox(int LayerId, object texte, object caption = null)
            {
                try
                {
                    IsWaitingUserAction = true;

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
                finally
                {
                    IsWaitingUserAction = false;
                }
            }

            static public void Alert(int LayerId, object texte, object caption = null)
            {
                try
                {
                    IsWaitingUserAction = true;

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
                finally
                {
                    IsWaitingUserAction = false;
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
                        
                        void callback()
                        {
                            ExecuteScript(Layers.GetLayerById(LayerId).class_script, null, LayerId, javascriptCallback?.ToString());
                        }

                        notification = new NText(texte.ToString(), caption?.ToString(), "MainPage", callback)
                        {
                            NotificationId = NotificationId
                        };
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
                stringBuilder.AppendLine(" - notification(\"message\", \"caption\", \"callback\", \"notificationId\") : Envoie une notification à l'écran. Un callback peut être attaché et appelé lors du clic sur celle-ci");
                stringBuilder.AppendLine(" - refreshMap() : Rafraîchit la carte à l'écran");
                stringBuilder.AppendLine(" - getStyle() : Obtient la valeur du style");
                stringBuilder.AppendLine(" - transformLocation(OriginWkt, TargetWkt, projX, projY) : Convertir la position X, Y d'un système de coordonnées vers un autre système (utilise Well Known Text definition)");
                stringBuilder.AppendLine(" - transformLocationFromWGS84(TargetWkt, Latitude, Longitude) : Convertir les coordonnées vers un autre système de coordonnées (utilise Well Known Text definition)");

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
