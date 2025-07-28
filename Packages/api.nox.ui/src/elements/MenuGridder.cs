using System.Linq;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.components {
	public class WidgetGrid : MonoBehaviour {
		public Vector2Int dimensions = new(1, 0);
		public float      spacing    = 0;

		void Start()
			=> UpdateLayout();

		void OnValidate()
			=> UpdateLayout();

		public Vector2 GetDimensions()
			=> GetDimensions(GetComponentsInChildren<WidgetGridItem>(true));

		private Vector2 GetDimensions(WidgetGridItem[] items) {
			var maxWidth  = GetMaxWidth(items);
			var maxHeight = GetMaxHeight(items);
			return new Vector2(maxWidth, maxHeight);
		}

		private int GetMaxWidth(WidgetGridItem[] items)
			=> dimensions.x == 0 ? items.Max(x => x.size.x) : dimensions.x;

		private int GetMaxHeight(WidgetGridItem[] items)
			=> dimensions.y == 0 ? items.Sum(x => x.size.y) : dimensions.y;

		private WidgetGridItem[] GetItems()
			=> GetComponentsInChildren<WidgetGridItem>(true);

		public void UpdateLayout()
			=> UpdateContent(GetItems());

		public void OnEnable()
			=> UpdateLayout();

		public void UpdateContent(WidgetGridItem[] items) {
			items = items.OrderBy(x => x.index).ToArray();
			Logger.LogDebug($"Updating grid with {items.Length} items");

			if (dimensions is { x: 0, y: 0 }) return;

			var rect = GetComponent<RectTransform>();
			if (items.Length == 0) {
				rect.sizeDelta = new Vector2(
					dimensions.x == 0 ? 0 : rect.sizeDelta.x,
					dimensions.y == 0 ? 0 : rect.sizeDelta.y
				);
				return;
			}

			var maxHeight = GetMaxHeight(items);
			var maxWidth  = GetMaxWidth(items);

			var calculated = new uint[maxWidth][];
			for (var x = 0; x < maxWidth; x++) {
				calculated[x] = new uint[maxHeight];
				for (var y = 0; y < maxHeight; y++)
					calculated[x][y] = uint.MaxValue;
			}

			foreach (var item in items) {
				if (item.flags.HasFlag(GridderItemFlags.ManualVisible) && !item.gameObject.activeInHierarchy)
					continue;

				var pos = new Vector2Int(maxWidth, maxHeight);
				if (!item.flags.HasFlag(GridderItemFlags.ManualPosition))
					for (uint i = 0; i < maxWidth * maxHeight; i++) {
						var x     = (int)(i % maxWidth);
						var y     = (int)(i / maxWidth);
						var found = true;

						for (var j = 0; j < item.size.x * item.size.y; j++) {
							var xx = x + j % item.size.x;
							var yy = y + j / item.size.x;
							if (xx >= maxWidth || yy >= maxHeight || calculated[xx][yy] != uint.MaxValue) {
								found = false;
								break;
							}

							pos = new Vector2Int(x, y);
						}

						if (found) break;
					}
				else pos = item.position;

				for (uint i = 0; i < item.size.x * item.size.y; i++) {
					var x = (uint)pos.x + i % (uint)item.size.x;
					var y = (uint)pos.y + i / (uint)item.size.x;

					if (x >= maxWidth || y >= maxHeight) continue;
					calculated[x][y] = item.index;
				}

				item.UpdatePosition(pos, new Vector2Int(maxWidth, maxHeight));
			}

			if (dimensions.y == 0) {
				var cellWidth     = rect.sizeDelta.x / maxWidth;
				var totalSpacingX = spacing          * (dimensions.x - 1);
				cellWidth -= totalSpacingX / dimensions.x;
				var height = items.Max(x => x.position.y + x.size.y);
				rect.sizeDelta = new Vector2(
					rect.sizeDelta.x,
					height * cellWidth + spacing * (height - 1)
				);
			} else if (dimensions.x == 0) {
				var cellHeight    = rect.sizeDelta.y / maxHeight;
				var totalSpacingY = spacing          * (dimensions.y - 1);
				cellHeight -= totalSpacingY / dimensions.y;
				var width = items.Max(x => x.position.x + x.size.x);
				rect.sizeDelta = new Vector2(
					width * cellHeight + spacing * (width - 1),
					rect.sizeDelta.y
				);
			}
		}
	}
}