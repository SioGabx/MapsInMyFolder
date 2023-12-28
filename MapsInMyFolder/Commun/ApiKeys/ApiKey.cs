namespace MapsInMyFolder.Commun
{
    public static partial class ApiKeys
    {
        //Setup here your API Keys.

        //If you want to contribute, you can protect your API Keys by adding a file named "ApiKey.Override.cs" in MapsInMyFolder.Commun. 
        //This file must contain this :
        //MAKE SURE THE FILE "ApiKey.Override.cs" IS INSIDE YOUR .gitignore BY ADDING THIS : "/MapsInMyFolder.Common/ApiKey.Override.cs"

        /*
        public static partial class ApiKeys
        {
            static ApiKeys()
            {
                BingMaps = "YOUR_REAL_API_KEY";
            }
        }
        */

        //Setup a BingMaps API Key here (need a Microsoft Account) : https://www.bingmapsportal.com/Account/Register
        public static string BingMaps { get; } = "YOUR_API_KEY";
    }
}
