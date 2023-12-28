using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder.Commun
{
    public abstract partial class Notification
    {
        public int InsertPosition = 0;
        public static event EventHandler<(string NotificationId, string Destinateur)> UpdateNotification;
        public static readonly List<Notification> ListOfNotificationsOnShow = new List<Notification>();
        public string NotificationId = "Notif" + DateTime.Now.Ticks.ToString();
        public string Destinateur = null;
        public bool DisappearAfterAMoment = false;
        public bool IsPersistant = false;
        public bool replaceOld = true;
        protected string Information = "";
        protected string Title = "";
        protected Action OnClickCallback = null;

        public Notification(string Information, string Title, string Destinateur, Action callback = null, bool doReplace = true)
        {
            this.Information = Information;
            this.Title = Title;
            this.OnClickCallback = callback;
            this.Destinateur = Destinateur;
            this.replaceOld = doReplace;
        }

        public virtual void Text(string Information = null, string Title = null)
        {
            if (!string.IsNullOrEmpty(Information))
            {
                this.Information = Information;
            }
            if (!string.IsNullOrEmpty(Title))
            {

                this.Title = Title;
            }
        }

        public void SendUpdate()
        {
            if (ListOfNotificationsOnShow.Contains(this))
            {
                UpdateNotification?.Invoke(this, (NotificationId, Destinateur));
            }
        }

        public bool Register()
        {
            if (ListOfNotificationsOnShow.Contains(this))
            {
                return false;
            }
            ListOfNotificationsOnShow.Add(this);
            SendUpdate();
            if (DisappearAfterAMoment)
            {
                Task.Delay(TimeSpan.FromSeconds(6)).ContinueWith(_ => Remove());
            }
            return true;
        }

        public void Remove()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ListOfNotificationsOnShow.Remove(this);
                UpdateNotification?.Invoke(null, (NotificationId, Destinateur));
            });
        }

        public static Notification GetById(string NotificationId)
        {
            return ListOfNotificationsOnShow.FirstOrDefault(notif => notif.NotificationId == NotificationId);
        }

        public static int Count()
        {
            return ListOfNotificationsOnShow.Count;
        }

        public virtual Grid Get()
        {
            Grid ContentGrid = Elements.ContentGrid(NotificationId, OnClickCallback, Remove);
            Border ContentBorder = Elements.ContentBorder();
            ContentBorder.Child = Elements.ContentTextBlock(Information, Title, OnClickCallback);
            ContentGrid.Children.Add(ContentBorder);
            ContentGrid.Children.Add(Elements.CloseButton(Remove));
            return ContentGrid;
        }
    }
}
