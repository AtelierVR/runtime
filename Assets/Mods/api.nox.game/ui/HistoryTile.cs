using UnityEngine;

namespace api.nox.game.UI
{
    public class HistoryTile : TileObject
    {
        public bool IsCurrent = false;
        public GameObject gameObject;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}