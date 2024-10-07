namespace api.nox.game.UI
{
    public class ViewPortMenu : Menu
    {
        internal ViewPortMenu() { }
        
        public override void Dispose()
        {
            base.Dispose();
            Destroy(gameObject);
        }
    }
}