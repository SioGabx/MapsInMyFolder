using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MapsInMyFolder.Commun
{
    public class Author
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string Node_id { get; set; }
        public string Avatar_url { get; set; }
        public string Gravatar_id { get; set; }
        public string Url { get; set; }
        public string Html_url { get; set; }
        public string Followers_url { get; set; }
        public string Following_url { get; set; }
        public string Gists_url { get; set; }
        public string Starred_url { get; set; }
        public string Subscriptions_url { get; set; }
        public string Organizations_url { get; set; }
        public string Repos_url { get; set; }
        public string Events_url { get; set; }
        public string Received_events_url { get; set; }
        public string Type { get; set; }
        public bool Site_admin { get; set; }
    }

    public class Uploader
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string Node_id { get; set; }
        public string Avatar_url { get; set; }
        public string Gravatar_id { get; set; }
        public string Url { get; set; }
        public string Html_url { get; set; }
        public string Followers_url { get; set; }
        public string Following_url { get; set; }
        public string Gists_url { get; set; }
        public string Starred_url { get; set; }
        public string Subscriptions_url { get; set; }
        public string Organizations_url { get; set; }
        public string Repos_url { get; set; }
        public string Events_url { get; set; }
        public string Received_events_url { get; set; }
        public string Type { get; set; }
        public bool Site_admin { get; set; }
    }

    public class Asset
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string Node_id { get; set; }
        public string Name { get; set; }
        public object Label { get; set; }
        public Uploader Uploader { get; set; }
        public string Content_type { get; set; }
        public string State { get; set; }
        public int Size { get; set; }
        public int Download_count { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        public string Browser_download_url { get; set; }
    }

    public class RootObject
    {
        public string Url { get; set; }
        public string Assets_url { get; set; }
        public string Upload_url { get; set; }
        public string Html_url { get; set; }
        public int Id { get; set; }
        public Author Author { get; set; }
        public string Node_id { get; set; }
        public string Tag_name { get; set; }
        public string Target_commitish { get; set; }
        public string Name { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Published_at { get; set; }
        public List<Asset> Assets { get; set; }
        public string Tarball_url { get; set; }
        public string Zipball_url { get; set; }
        public string Body { get; set; }
    }

    public class GitHubFile
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Sha { get; set; }
        public int Size { get; set; }
        public string Url { get; set; }
        public string Html_url { get; set; }
        public string Git_url { get; set; }
        public string Download_url { get; set; }
        public string Type { get; set; }
        public GitHubFileLinks Links { get; set; }
    }

    public class GitHubFileLinks
    {
        public string Self { get; set; }
        public string Git { get; set; }
        public string Html { get; set; }
    }


    public static class GetGithubAssets
    {
        public static Asset GetReleaseAssetsFromGithub(string githubUrl, string filename)
        {
            string url = "https://api.github.com/repos" + githubUrl + "/releases";
            string responseBody = GetAssetsFromGithub(url, filename);
            if (string.IsNullOrEmpty(responseBody))
            {
                var token = JToken.Parse(responseBody);
                List<RootObject> releases;
                if (token is JArray)
                {
                    releases = JsonConvert.DeserializeObject<List<RootObject>>(responseBody);
                }
                else if (token is JObject)
                {
                    releases = new List<RootObject>
                    {
                        JsonConvert.DeserializeObject<RootObject>(responseBody)
                    };
                }
                else
                {
                    releases = new List<RootObject>();
                }

                foreach (var release in releases)
                {
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name == filename)
                        {
                            string AssetSerializeObject = JsonConvert.SerializeObject(asset);
                            XMLParser.Write("GithubAsset_" + filename, AssetSerializeObject);
                            return asset;
                        }

                    }
                }
                return new Asset { Created_at = new DateTime(0), Browser_download_url = "File not found in assets :" + filename };
            }
            return new Asset { Created_at = new DateTime(0), Browser_download_url = "Error" + filename };
        }


        public static GitHubFile GetContentAssetsFromGithub(string githubUrl, string githubPath, string filename)
        {
            //$"https://api.github.com/repos/SioGabx/MapsInMyFolder/contents/MapsInMyFolder/cursors/
            string url = "https://api.github.com/repos" + githubUrl + "/contents/" + githubPath;
            string responseBody = GetAssetsFromGithub(url, filename);
            if (!string.IsNullOrEmpty(responseBody))
            {
                var token = JToken.Parse(responseBody);
                List<GitHubFile> files;
                if (token is JArray)
                {
                    files = JsonConvert.DeserializeObject<List<GitHubFile>>(responseBody);
                }
                else if (token is JObject)
                {
                    files = new List<GitHubFile>
                    {
                        JsonConvert.DeserializeObject<GitHubFile>(responseBody)
                    };
                }
                else
                {
                    files = new List<GitHubFile>();
                }



                foreach (var file in files)
                {
                    Debug.WriteLine(file.Name);
                    if (file.Name == filename)
                    {
                        string AssetSerializeObject = JsonConvert.SerializeObject(file);
                        XMLParser.Write("GithubAsset_" + filename, AssetSerializeObject);
                        return file;
                    }
                }
                Debug.WriteLine("Fichier non trouvé : " + filename);
            }
            return null;
        }



        public static string GetAssetsFromGithub(string url, string filename)
        {
            //$"https://api.github.com/repos/SioGabx/MapsInMyFolder/releases/latest"
            //$"https://api.github.com/repos/SioGabx/MapsInMyFolder/contents/MapsInMyFolder/cursors/

            Debug.WriteLine(url);
            string ETag = XMLParser.Read("ETag_" + filename);
            if (!string.IsNullOrEmpty(ETag))
            {
                TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Add("If-None-Match", ETag);
            }
            Commun.HttpResponse HttpResponse = Collectif.ByteDownloadUri(new Uri(url), 0, true).Result;
            if (TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Contains("If-None-Match"))
            {
                TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Remove("If-None-Match");
            }

            if (HttpResponse.ResponseMessage.IsSuccessStatusCode)
            {
                ETag = HttpResponse.ResponseMessage.Headers.TryGetValues("etag", out var values) ? values.FirstOrDefault() : null;
                XMLParser.Write("ETag_" + filename, ETag);
            }
            else
            {
                Debug.WriteLine(HttpResponse.ResponseMessage.StatusCode);
                if (HttpResponse.ResponseMessage.StatusCode == HttpStatusCode.NotModified)
                {
                    return XMLParser.Read("GithubAsset_" + filename);
                }
                else
                {
                    return string.Empty;
                }
            }
            return Collectif.ByteArrayToString(HttpResponse.Buffer);
        }
    }
}
