using System.Collections.Generic;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class NotificationManager
    {
        public static List<Notification> Notifications = new List<Notification>();
        public static void Add(Notification notification) => Notifications.Add(notification);
        public static void Remove(Notification notification) => Notifications.Remove(notification);
        public static void Remove(string uid) => GetMany(uid).ForEach(n => Remove(n));
        public static Notification Get(string uid) => Notifications.Find(n => n.Uid == uid);
        public static List<Notification> GetMany(string uid)
        {
            return uid.StartsWith("*") ?
                Notifications.FindAll(n => n.Uid.EndsWith(uid[1..]))
                : uid.EndsWith("*") ?
                Notifications.FindAll(n => n.Uid.StartsWith(uid[..^1]))
                : Notifications.FindAll(n => n.Uid == uid);
        }
        public static bool Has(string uid) => GetMany(uid).Count > 0;
        public static void Clear() => Notifications.Clear();

        public static void Set(Notification notification)
        {
            Remove(notification.Uid);
            Add(notification);
        }
    }

    public class Notification
    {
        public string Uid;
        public NotificationType Type;
        public VisualElement Content;
        public List<VisualElement> Actions = new();
    }

    public enum NotificationType
    {
        Good,
        Warning,
        Error,
        Info,
        None
    }
}