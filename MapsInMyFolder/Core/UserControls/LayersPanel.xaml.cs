using CefSharp;
using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour LayersPanel.xaml
    /// </summary>
    public partial class LayersPanel : UserControl
    {
        public LayersPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LinkedMapViewerProperty =
          DependencyProperty.Register(
              "LinkedMapViewer",
              typeof(MapControl.Map),
              typeof(LayersPanel),
              new PropertyMetadata(null));

        public MapControl.Map LinkedMapViewer
        {
            get { return (MapControl.Map)GetValue(LinkedMapViewerProperty); }
            set { SetValue(LinkedMapViewerProperty, value); }
        }

        public class LayerIdEventArgs : EventArgs
        {
            public int LayerId { get; }

            public LayerIdEventArgs(int LayerId)
            {
                this.LayerId = LayerId;
            }
        }
        public delegate void LayerIdEventHandler(object sender, LayerIdEventArgs e);

        public event LayerIdEventHandler SetCurrentLayerEvent;

        public virtual void OnSetCurrentLayerEvent(int LayerId)
        {
            SetCurrentLayerEvent?.Invoke(this, new LayerIdEventArgs(LayerId));
        }



        public void InitLayerPanel()
        {
            if (LayerBrowser is null) { return; }

            var requestHandler = new CustomRequestHandler();
            LayerBrowser.RequestHandler = requestHandler;

            try
            {
                LayerBrowser.JavascriptObjectRepository.Register("LayerCEFSharpLink", new LayerLink(this, LinkedMapViewer));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            try
            {
                LayerBrowser.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"LayerCEFSharpLink\");");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Refresh()
        {
            InitLayerPanel();
            ReloadPage();
            LayersSearchBar.SearchLayerStart();
        }
        public void ReloadPage()
        {
            LayerBrowser.LoadHtml(LayersLoad(), "http://siogabx.fr");
        }

        private void LayerBrowser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                LayerBrowser.GetMainFrame().EvaluateScriptAsync(LayerGetDefaultSelectByIdScript());
                if (Settings.show_layer_devtool)
                {
                    LayerBrowser.ShowDevTools();
                }
            }
        }


        public static string LayerGetDefaultSelectByIdScript()
        {
            int LayerId = Layers.Current.Id;
            string scroll = ", true";
            if (LayerId == -1)
            {
                LayerId = Layers.StartupLayerId;
                scroll = "";
            }
            return $"lastelement = document.getElementById({LayerId}); selectionner_calque_by_id({LayerId}{scroll})";
        }

        public void PreviewRequestUpdate()
        {
            LayerBrowser.ExecuteScriptAsyncWhenPageLoaded("UpdatePreview();");
            return;
        }

        static string LayersLoad()
        {
            Layers.Load();
            if (Database.DB_IsConnectionNull())
            {
                return "<style>p{font-family: \"Segoe UI\";color:#888989;font-size:14px;}</style><p>" + Languages.Current["layerMessageNoDatabase"] + "</p>";
            }
            StringBuilder PropertyBuilder = new StringBuilder();
            string baseHTML = LayersCreateHTML();
            PropertyBuilder.Append(baseHTML);
            PropertyBuilder.Append("<script>");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--opacity_preview_background-image\", {Settings.background_layer_opacity});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_R\", {Settings.background_layer_color_R});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_G\", {Settings.background_layer_color_G});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_B\", {Settings.background_layer_color_B});");
            PropertyBuilder.Append("</script>");
            return PropertyBuilder.ToString();
        }


        static string LayersCreateHTML()
        {

            StringBuilder generated_layers = new StringBuilder("<ul class=\"");
            generated_layers.Append(Settings.layerpanel_displaystyle.ToString().ToLower());
            generated_layers.AppendLine("\">");

            List<Layers> layersRejectedAtFirstIteration = new List<Layers>();
            string[] layersSpecificsCountryToKeep = Settings.filter_layers_based_on_country.Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int iterationOfLayerTreatments = 0; iterationOfLayerTreatments <= 1; iterationOfLayerTreatments++)
            {
                bool isFirstIterationDoRejectLayer = iterationOfLayerTreatments == 0;
                IEnumerable<Layers> EnumerableLayers;

                if (isFirstIterationDoRejectLayer)
                {
                    EnumerableLayers = Layers.GetLayersList();
                }
                else
                {
                    EnumerableLayers = layersRejectedAtFirstIteration;
                }

                foreach (Layers layer in EnumerableLayers)
                {
                    if (isFirstIterationDoRejectLayer && Settings.layerpanel_put_non_letter_layername_at_the_end)
                    {
                        if (string.IsNullOrEmpty(layer.Name) || !Char.IsLetter(layer.Name.Trim()[0]))
                        {
                            layersRejectedAtFirstIteration.Add(layer);
                            continue;
                        }
                    }

                    bool LayerShouldBeCountryFiltered = true;


                    string[] layerCountrySpecificsAttributes = layer.Country.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    string[] GlobeSpecificAttributes = new string[] { "Invariant Country", "World", "*" };

                    bool isGlobeSpecificLayer = layerCountrySpecificsAttributes.ContainsOneOrMore(GlobeSpecificAttributes);

                    if (layersSpecificsCountryToKeep.Length == 0 || layerCountrySpecificsAttributes.Length == 0 || string.IsNullOrWhiteSpace(layer.Country))
                    {
                        LayerShouldBeCountryFiltered = false;
                    }
                    else
                    {
                        if (layersSpecificsCountryToKeep.ContainsOneOrMore(GlobeSpecificAttributes) && isGlobeSpecificLayer)
                        {
                            LayerShouldBeCountryFiltered = false;
                        }
                        if (layerCountrySpecificsAttributes.ContainsOneOrMore(layersSpecificsCountryToKeep))
                        {
                            LayerShouldBeCountryFiltered = false;
                        }
                    }

                    bool ShowCountry = true;
                    string CountryHTML = string.Empty;
                    if (ShowCountry)
                    {
                        CountryHTML = " - " + layer.Country;
                    }

                    string layerVisibilityHTML;
                    string layerFiltered = "layer";
                    if (LayerShouldBeCountryFiltered)
                    {
                        layerFiltered += "Filtered";
                        layerVisibilityHTML = @"class=""eye hidden""";
                        continue;
                    }
                    else
                    {
                        if (layer.Visibility == "Hidden")
                        {
                            layerVisibilityHTML = @$"class=""eye"" title=""{Languages.Current["layerContextMenuShowLayer"]}""";
                            layerFiltered += "Hidden";
                        }
                        else
                        {
                            layerVisibilityHTML = @$"class=""eye orange"" title=""{Languages.Current["layerContextMenuHideLayer"]}""";
                            layerFiltered += "Visible";
                        }
                    }
                    string layerFavoriteHTML = layer.IsFavorite
                       ? @$"class=""star orange"" title=""{Languages.Current["layerContextMenuRemoveFavorite"]}"""
                       : @$"class=""star"" title=""{Languages.Current["layerContextMenuAddFavorite"]}""";



                    string overideBackgroundColor = string.Empty;
                    if (!string.IsNullOrEmpty(layer?.SpecialsOptions?.BackgroundColor?.Trim()))
                    {
                        var Color = Collectif.HexValueToSolidColorBrush(layer.SpecialsOptions.BackgroundColor);
                        overideBackgroundColor = $"background-color:rgba({Color.Color.R},{Color.Color.G},{Color.Color.B},{1 - Settings.background_layer_opacity});";
                    }
                    string supplement_class = string.Empty;
                    if (!Settings.layerpanel_website_IsVisible)
                    {
                        supplement_class = string.Concat(" ", "displaynone");
                    }

                    string WarningMessageDiv = string.Empty;
                    if (layer.DoShowWarningLegacyVersionNewerThanEdited)
                    {
                        WarningMessageDiv = $"<div class=\"warning\" title=\"{Languages.Current["layerMessageErrorDetectedClickHere"]}\" onclick=\"show_warning(event, '{layer.Id}');\"></div>";
                    }

                    generated_layers.AppendLine(@$"
                <li class=""{layerFiltered}"" id=""{layer.Id}"">
                    <div class=""layer_main_div"" style=""{overideBackgroundColor}"">
                        <div class=""layer_main_div_preview_images"">
                            <div class=""layer_main_div_background_image""></div>
                            <div class=""layer_main_div_front_image""></div>
                        </div>
                        <div class=""layer_content"" data-layer=""{layer.Identifier}"" title=""{Collectif.HTMLEntities(layer.Description)}"">
                            <div class=""layer_texte"">
                                <p class=""display_name"">{Collectif.HTMLEntities(layer.Name)}</p>
                                <p class=""zoom"">[{layer.MinZoom}-{layer.MaxZoom}]{CountryHTML} - {layer.SiteName}</p>
                                <p class=""layer_website{supplement_class}"">{layer.SiteName}</p>
                                <p class=""layer_category{supplement_class}"">{layer.Tag}</p>
                            </div>
                            <div {layerFavoriteHTML} onclick=""ajouter_aux_favoris(event, this, {layer.Id})""></div>
                            <div {layerVisibilityHTML} onclick=""change_visibility(event, this, {layer.Id})""></div>
                            {WarningMessageDiv}
                        </div>
                    </div>
                </li>");
                }
            }

            generated_layers.AppendLine("</ul>");
            generated_layers.AppendLine($"<script>{LayerGetDefaultSelectByIdScript()}</script>");
            string resource_data = Collectif.ReadResourceString("Core/Chromium/HTML/layer_panel.html");
            resource_data = Languages.ReplaceInString(resource_data);
            return resource_data.Replace("<!--htmllayerplaceholder-->", generated_layers.ToString());
        }

        private void LayerBrowser_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            e.Handled = true;
        }



    }
}
