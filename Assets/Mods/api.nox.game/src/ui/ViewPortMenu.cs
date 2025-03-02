using System;
using Nox.CCK.Utils;

namespace api.nox.game.UI
{
    public class ViewPortMenu : Menu
    {
        public override void Dispose()
        {
            base.Dispose();
            Destroy(gameObject);
        }

        public void OnDestroy()
        {
            Logger.Log("Destroying ViewPortMenu");
        }
    }
}