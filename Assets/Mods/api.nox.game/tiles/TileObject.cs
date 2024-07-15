
using System;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    public class TileObject : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public bool showNavigation = true;
        [ShareObjectImport, ShareObjectExport] public bool isReforced = false; // If true, the tile can't be removed, except by an other renforced tile
        [ShareObjectImport, ShareObjectExport] public bool addToHistory = true; // If true, the tile will be added to the history, and can be restored
        [ShareObjectImport, ShareObjectExport] public string id;
        public GameObject content;
        [ShareObjectImport, ShareObjectExport] public Func<Transform, GameObject> GetContent;
        [ShareObjectImport, ShareObjectExport] public Action<string> onOpen = null; // Called when the tile is opened at the first time (before reading the content) (the string is the previous tile id)
        [ShareObjectImport, ShareObjectExport] public Action<string> onRestore = null; // Called when the tile is restored (before reading the content) (the string is the previous tile id)
        [ShareObjectImport, ShareObjectExport] public Action onRemove = null; // Called when the tile is removed (after hiding the content) (the string is the next tile id)
        [ShareObjectImport, ShareObjectExport] public Action<string> onDisplay = null; // Called when the tile is displayed (after reading the content) (the string is the previous tile id)
        [ShareObjectImport, ShareObjectExport] public Action<string> onHide = null; // Called when the tile is hidden (after hiding the content) (the string is the next tile id)

        // first time: [onOpen] -> [onDisplay] -> (display shown)
        // on restore: [onRestore] -> [onDisplay] -> (display shown)
        // on remove (no history): (display hidded) -> [onHide] -> [onRemove]
        // on hide (history): (display hidded) -> [onHide]
    }
}