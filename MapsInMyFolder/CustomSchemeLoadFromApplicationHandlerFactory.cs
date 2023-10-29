using CefSharp;
using System;
using System.Web;

namespace MapsInMyFolder
{
    //public class CustomResourceRequestHandler : ResourceRequestHandler
    //{
    //    protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
    //    {
    //        if (request.ResourceType == ResourceType.Image)
    //        {
    //            request.SetReferrer("https://example.com", ReferrerPolicy.Origin);

    //            string originalUrl = request.Url;
    //            string modifiedUrl = "https://example.com/modified-image.jpg";
    //            request.Url = modifiedUrl;
    //        }

    //        return CefReturnValue.Continue;
    //    }
    //}

    public class CustomRequestHandler : CefSharp.Handler.RequestHandler
    {
        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {

            if (request.Url.StartsWith("mapsinmyfolder://get"))
            {
                return new CustomResourceRequestHandler();
            }

            return null;
        }
    }

    public class CustomResourceRequestHandler : CefSharp.Handler.ResourceRequestHandler
    {
        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var uri = new Uri(request.Url);
            string query = uri.Query; // "?param1=value1&param2=value2"

            // Vous pouvez utiliser la classe HttpUtility.ParseQueryString pour analyser les paramètres
            var queryParams = HttpUtility.ParseQueryString(query);

            string argsUrl = queryParams["url"];
            string argsReferrer = queryParams["referrer"];

            request.SetReferrer(argsReferrer, ReferrerPolicy.Origin);
            request.Url = argsUrl;
            return CefReturnValue.Continue;
        }
    }








    //public class CustomSchemeLoadFromApplicationHandlerFactory : ISchemeHandlerFactory
    //{
    //    public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
    //    {
    //        if (schemeName == "mapsinmyfolder")
    //        {
    //            string url = request.Url;
    //            if (url.StartsWith("mapsinmyfolder://get"))
    //            {
    //                var uri = new Uri(url);
    //                string query = uri.Query; // "?param1=value1&param2=value2"

    //                // Vous pouvez utiliser la classe HttpUtility.ParseQueryString pour analyser les paramètres
    //                var queryParams = HttpUtility.ParseQueryString(query);

    //                // Accédez aux valeurs des paramètres individuellement
    //                if (!int.TryParse(queryParams["id"], out int argsid))
    //                {
    //                    return CreateErrorResponse("Invalid ID");
    //                }

    //                string argsurl = HttpUtility.UrlDecode(queryParams["url"]).Replace("[internal]", string.Empty);

    //                HttpResponse loadedPreview = Task.Run(async () => await Collectif.ByteDownloadUri(new Uri(Collectif.AddHttpToUrl(argsurl)), argsid, true)).Result;
    //                if (!loadedPreview?.ResponseMessage?.IsSuccessStatusCode == true)
    //                {
    //                    return null;// CreateErrorResponse(loadedPreview.ResponseMessage.StatusCode + loadedPreview.ResponseMessage.ReasonPhrase);
    //                }
    //                byte[] imageData = loadedPreview?.Buffer;
    //                if (imageData != null)
    //                {
    //                    MemoryStream stream = new MemoryStream(imageData);
    //                    return ResourceHandler.FromStream(stream, ".png");
    //                }
    //                else
    //                {
    //                    return null;// CreateErrorResponse("Failed to load image");
    //                }
    //            }
    //        }

    //        return null;
    //    }

    //    private IResourceHandler CreateErrorResponse(string errorMessage)
    //    {
    //        string errorContent = "Error: " + errorMessage;
    //        byte[] errorData = Encoding.UTF8.GetBytes(errorContent);

    //        MemoryStream stream = new MemoryStream(errorData);
    //        return ResourceHandler.FromStream(stream, ".html");
    //    }

    //}
}