using System;

namespace MapsInMyFolder.Commun
{
    public class NText : Notification
    {
        public NText(string Information, string Title, string Destinateur, Action callback = null, bool replaceOld = true) : base(Information, Title, Destinateur, callback, replaceOld) { }
    }
}
