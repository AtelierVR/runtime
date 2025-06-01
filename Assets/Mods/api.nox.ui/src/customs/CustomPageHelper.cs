using Nox.CCK.Mods.Events;

namespace api.nox.ui.pages
{
    public abstract class CustomPageHelper
    {
        protected CustomPageHelper()
            => _subscription = UISystem.CoreAPI.EventAPI.Subscribe("goto_page", OnGotoEvent);

        private void OnGotoEvent(EventData context)
        {
            if (!context.TryGet(0, out int menuId) || !context.TryGet(1, out string pageKey) || context.Data.Length < 2)
                return;
            if (pageKey != GetKey()) return;
            OnGotoEvent(menuId, context.Data[2..]);
        }

        protected abstract string GetKey();
        protected abstract void OnGotoEvent(int menuId, object[] args);

        private EventSubscription _subscription;

        public virtual void Dispose()
        {
            UISystem.CoreAPI.EventAPI.Unsubscribe(_subscription);
            _subscription = null;
        }
    }
}