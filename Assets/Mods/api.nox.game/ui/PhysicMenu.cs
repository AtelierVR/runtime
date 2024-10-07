namespace api.nox.game.UI
{
    public class PhysicMenu : Menu
    {
        public bool IsFixed = true;

        public override void Dispose()
        {
            base.Dispose();
            Destroy(gameObject);
        }
    }
}