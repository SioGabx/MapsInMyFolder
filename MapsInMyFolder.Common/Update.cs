namespace MapsInMyFolder.Commun
{
    public static class Update
    {
        static bool CheckIfNewerVersionAvailableOnGithub()
        {
            if (Network.IsNetworkAvailable()) { return false; }
            //https://github.com/microsoft/PowerToys/blob/e5c3b15a458950964c46cec1fa9c22cf5da72f4e/src/common/updating/updating.cpp
            //https://api.github.com/repos/microsoft/PowerToys/releases/latest

            return false;
        }
    }
}
