using System.Linq;
using UnityEngine;

namespace api.nox.game
{
    public class MenuGridder : MonoBehaviour
    {
        public Vector2 dimensions = new(1, 1);
        void Start() => UpdateContent();
        void OnValidate() => UpdateContent();

        public void UpdateContent()
        {
            var items = GetComponentsInChildren<MenuGridderItem>(true).OrderBy(x => x.index).ToArray();

            uint[][] calculated = new uint[(int)dimensions.x][];
            for (int x = 0; x < dimensions.x; x++)
            {
                calculated[x] = new uint[(int)dimensions.y];
                for (int y = 0; y < dimensions.y; y++)
                    calculated[x][y] = uint.MaxValue;
            }

            foreach (var item in items)
            {
                if (item.flags.HasFlag(GridderItemFlags.ManualVisible) && !item.gameObject.activeInHierarchy)
                    continue;

                var pos = new Vector2(uint.MaxValue, float.MaxValue);
                if (!item.flags.HasFlag(GridderItemFlags.ManualPosition))
                    for (uint i = 0; i < dimensions.x * dimensions.y; i++)
                    {
                        var x = i % (int)dimensions.x;
                        var y = i / (int)dimensions.x;
                        var found = true;

                        for (var j = 0; j < item.size.x * item.size.y; j++)
                        {
                            var xx = x + j % (int)item.size.x;
                            var yy = y + j / (int)item.size.x;
                            if (xx >= dimensions.x || yy >= dimensions.y || calculated[xx][yy] != uint.MaxValue)
                            {
                                found = false;
                                break;
                            }
                            else pos = new Vector2(x, y);
                        }

                        if (found) break;
                    }
                else pos = item.position;

                if (pos.x == uint.MaxValue || pos.y == uint.MaxValue) continue;

                for (uint i = 0; i < item.size.x * item.size.y; i++)
                {
                    var x = (uint)pos.x + i % (uint)item.size.x;
                    var y = (uint)pos.y + i / (uint)item.size.x;

                    if (x >= dimensions.x || y >= dimensions.y) continue;
                    calculated[x][y] = item.index;
                }

                item.UpdatePosition(pos);
            }

        }
    }
}