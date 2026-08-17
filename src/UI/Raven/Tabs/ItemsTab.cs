using System.Collections.Generic;
using System.Linq;
using EFT.Interactive;
using EFT.InventoryLogic;
using RavenX.Extensions;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class ItemsTab : IRavenTab
{
	public string Title => "Items";

	private string _trackInput = string.Empty;
	private string _searchInput = string.Empty;
	private string _status = string.Empty;

	private string? _lastQuery;
	private Entry[] _results = [];
	private Vector2 _scroll;
	private Entry? _selected;

	private sealed class Entry
	{
		public string Name = string.Empty;
		public string ShortName = string.Empty;
		public string Id = string.Empty;

		private string? _description;
		private string? _category;
		public ItemTemplate? Template;

		public string Description => _description ??= Template?.DescriptionLocalizationKey.Localized() ?? string.Empty;

		public string Category => _category ??= Template != null ? TemplateHelper.GetCategory(Template) : "Other";

		private string? _subcategory;

		public string Subcategory => _subcategory ??= Template != null ? TemplateHelper.GetSubcategory(Template) : string.Empty;
	}

	private string[] _categories = ["All"];
	private int _category;
	private int _sub;

	private string _liveFilter = string.Empty;
	private Vector2 _liveScroll;

	public void Draw()
	{
		_sub = RavenWidgets.SubTabBar(["Catalogue", "Live Items", "Tracked"], _sub);

		switch (_sub)
		{
			case 0:
				DrawSpawnCard();
				break;
			case 1:
				RavenTabHelper.BeginColumns();
				RavenTabHelper.BeginColumn(560f);
				DrawLiveItemsCard();
				RavenTabHelper.EndColumn();
				RavenTabHelper.EndColumns();
				break;
			default:
				RavenTabHelper.BeginColumns();
				RavenTabHelper.BeginColumn(420f);
				DrawTrackedCard();
				RavenTabHelper.EndColumn();
				RavenTabHelper.BeginColumn(320f);
				DrawFiltersCard();
				RavenTabHelper.EndColumn();
				RavenTabHelper.EndColumns();
				break;
		}
	}

	private void DrawLiveItemsCard()
	{
		using (RavenMenu.Card("Loot In Raid"))
		{
			var player = GameState.Current?.LocalPlayer;
			var world = Comfort.Common.Singleton<GameWorld>.Instance;

			if (!player.IsValid() || world?.LootItems == null)
			{
				GUILayout.Label("Not in a raid.", RavenTheme.MutedLabel);
				return;
			}

			_liveFilter = RavenWidgets.TextField(_liveFilter, "search by name");
			RavenWidgets.Spacer(6f);

			var origin = player.Transform.position;
			var needle = _liveFilter.Trim();
			var found = new List<(string name, string kind, float distance, Vector3 position)>();
			var loot = world.LootItems;

			for (var i = 0; i < loot.Count; i++)
			{
				var lootItem = loot.GetByIndex(i);
				if (!lootItem.IsValid())
					continue;

				string name;
				string kind;

				if (lootItem is Corpse corpse)
				{
					name = corpse.ItemOwner?.RootItem?.ShortName.Localized() ?? "Corpse";
					kind = "corpse";
				}
				else
				{
					name = lootItem.Item?.ShortName.Localized() ?? string.Empty;
					kind = "item";
				}

				if (name.Length == 0)
					continue;

				if (needle.Length > 0
					&& name.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) < 0
					&& kind.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) < 0)
					continue;

				var position = lootItem.transform.position;
				found.Add((name, kind, Vector3.Distance(origin, position), position));
			}

			if (found.Count == 0)
			{
				GUILayout.Label(needle.Length == 0 ? "Nothing lootable found." : "Nothing matches.", RavenTheme.MutedLabel);
				return;
			}

			var corpses = found.Count(x => x.kind == "corpse");
			GUILayout.Label($"{found.Count - corpses} item(s), {corpses} corpse(s)", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(4f);

			_liveScroll = GUILayout.BeginScrollView(_liveScroll, false, true, GUILayout.Height(360f));

			foreach (var (name, kind, distance, position) in found.OrderBy(x => x.distance).Take(200))
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
				GUILayout.Label(name, RavenTheme.Label, GUILayout.ExpandWidth(true));

				if (kind == "corpse")
					GUILayout.Label("corpse", RavenTheme.ValueLabel, GUILayout.Width(54f));

				GUILayout.Label($"{distance:0}m", RavenTheme.ValueLabel, GUILayout.Width(58f));

				if (RavenWidgets.SmallButton("goto", 48f))
					player.Teleport(position + Vector3.up * 0.4f, false);

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}
	}

	private static void DrawFiltersCard()
	{
		using (RavenMenu.Card("Filters"))
		{
			var loot = FeatureFactory.GetFeature<LootItems>();
			if (loot == null)
			{
				GUILayout.Label("Loot items feature unavailable.", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.Label("Applies on top of what you track. Zero means no limit.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(8f);

			var price = RavenWidgets.Slider("Minimum price", loot.MinimumPrice, 0f, 100000f,
				loot.MinimumPrice > 0 ? $"{loot.MinimumPrice}" : "off");
			loot.MinimumPrice = Mathf.RoundToInt(price / 500f) * 500;

			RavenWidgets.Spacer(6f);

			var distance = RavenWidgets.Slider("Maximum distance", loot.MaximumDistance, 0f, 500f,
				loot.MaximumDistance > 0 ? $"{loot.MaximumDistance:0} m" : "off");
			loot.MaximumDistance = Mathf.Round(distance / 10f) * 10f;
		}
	}

	private void DrawTrackedCard()
	{
		using (RavenMenu.Card("Tracked Items"))
		{
			var loot = FeatureFactory.GetFeature<LootItems>();
			if (loot == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.BeginHorizontal();
			_trackInput = RavenWidgets.TextField(_trackInput, "name or * for everything");
			GUILayout.Space(6f);
			var add = RavenWidgets.OutlineButton("ADD", 62f);
			GUILayout.EndHorizontal();

			if (add && _trackInput.Trim().Length > 0)
			{
				var name = _trackInput.Trim();
				if (!loot.TrackedNames.Any(t => t.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase)))
					loot.TrackedNames.Add(new TrackedItem(name));

				_trackInput = string.Empty;
			}

			RavenWidgets.Spacer(8f);

			if (loot.TrackedNames.Count == 0)
			{
				GUILayout.Label("Nothing tracked.", RavenTheme.MutedLabel);
				return;
			}

			foreach (var tracked in loot.TrackedNames.ToArray())
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));

				var swatch = GUILayoutUtility.GetRect(13f, RavenTheme.RowHeight, GUILayout.Width(13f));
				if (tracked.Color is { } color)
					RavenWidgets.Rounded(new Rect(swatch.x, swatch.y + 5f, 13f, 13f), 2f, color, color);

				GUILayout.Space(8f);
				GUILayout.Label(tracked.IsMatchAll ? "everything" : tracked.Name, RavenTheme.Label, GUILayout.ExpandWidth(true));

				if (tracked.Rarity is { } rarity)
					GUILayout.Label(rarity.ToString(), RavenTheme.ValueLabel, GUILayout.Width(74f));

				if (RavenWidgets.SmallButton("remove", 58f))
					loot.TrackedNames.Remove(tracked);

				GUILayout.EndHorizontal();
			}
		}
	}

	private void DrawSpawnCard()
	{
		_searchInput = RavenWidgets.TextField(_searchInput, "filter by name — empty shows everything");
		RefreshResults();
		RavenWidgets.Spacer(10f);

		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(230f);
		DrawCategoryTree();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(250f);
		DrawResultList();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(320f);
		DrawDetail();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static readonly System.Collections.Generic.List<CatalogCategory> _visibleCategories = [];
	private Vector2 _treeScroll;
	private string _categoryId = string.Empty;
	private string _categoryName = string.Empty;

	private void DrawCategoryTree()
	{
		using (RavenMenu.Card("Categories"))
		{
			HandbookCatalog.Refresh();

			if (!HandbookCatalog.Ready)
			{
				GUILayout.Label("Handbook not loaded yet.\nOpen it once in the main menu.", RavenTheme.MutedLabel);
				return;
			}

			if (RavenWidgets.OutlineButton("ALL ITEMS", 120f))
			{
				_categoryId = string.Empty;
				_categoryName = string.Empty;
			}

			RavenWidgets.Spacer(6f);

			HandbookCatalog.Flatten(_visibleCategories);

			_treeScroll = GUILayout.BeginScrollView(_treeScroll, false, true, GUILayout.Height(340f));

			foreach (var category in _visibleCategories)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
				GUILayout.Space(category.Depth * 12f);

				if (category.Children.Count > 0)
				{
					if (GUILayout.Button(category.Expanded ? "-" : "+", RavenTheme.ValueLabel, GUILayout.Width(14f)))
						category.Expanded = !category.Expanded;
				}
				else
				{
					GUILayout.Space(14f);
				}

				var selected = category.Id == _categoryId;

				GUI.contentColor = selected ? RavenTheme.Accent : Color.white;

				if (GUILayout.Button(category.Name, RavenTheme.Label, GUILayout.ExpandWidth(true)))
				{
					_categoryId = selected ? string.Empty : category.Id;
					_categoryName = selected ? string.Empty : category.Name;
				}

				GUI.contentColor = Color.white;
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();

			RavenWidgets.Spacer(4f);
			GUILayout.Label(_categoryName.Length > 0 ? _categoryName : "All items", RavenTheme.MutedLabel);
		}
	}

	private void DrawResultList()
	{
		using (RavenMenu.Card("Results"))
		{
			if (_results.Length == 0)
			{
				GUILayout.Label(_searchInput.Trim().Length == 0
					? "Item database not loaded yet."
					: "No item matches.", RavenTheme.MutedLabel);
				return;
			}

			var rows = FilteredResults();
			GUILayout.Label($"{rows.Length} items", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(4f);

			_scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.Height(380f));

			var rowHeight = RavenTheme.RowHeight;
			var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / rowHeight) - 2);
			var visible = Mathf.CeilToInt(380f / rowHeight) + 4;

			GUILayout.Space(first * rowHeight);

			foreach (var entry in rows.Skip(first).Take(visible))
			{
				var rect = GUILayoutUtility.GetRect(GUIContent.none, RavenTheme.Label, GUILayout.Height(RavenTheme.RowHeight));
				var isSelected = ReferenceEquals(_selected, entry);

				if (isSelected)
					RavenWidgets.Rounded(rect, 3f, RavenTheme.AccentTrack, RavenTheme.AccentTrack);

				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
				{
					_selected = entry;
					Event.current.Use();
				}

				var caption = entry.Name.Length > 0 ? entry.Name : entry.ShortName;
				GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), caption, RavenWidgets.RowLabel(isSelected));
			}

			GUILayout.Space(Mathf.Max(0, rows.Length - first - visible) * rowHeight);

			GUILayout.EndScrollView();
		}
	}

	private void DrawDetail()
	{
		using (RavenMenu.Card("Details"))
		{
			if (_selected == null)
			{
				GUILayout.Label("Select an item on the left.", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.Label(_selected.Name.Length > 0 ? _selected.Name : _selected.ShortName, RavenTheme.Title);

			var path = HandbookCatalog.PathOf(_selected.Id);

			if (path.Length == 0)
				path = _selected.Subcategory.Length > 0 && _selected.Subcategory != _selected.Category
					? $"{_selected.Category}  ›  {_selected.Subcategory}"
					: _selected.Category;

			GUILayout.Label(path, RavenTheme.MutedLabel);
			GUILayout.Label(_selected.Id, RavenTheme.MutedLabel);

			var price = HandbookCatalog.PriceOf(_selected.Id);
			if (price > 0f)
				GUILayout.Label($"Handbook value  {price:N0} ₽", RavenTheme.ValueLabel);
			RavenWidgets.Spacer(8f);

			if (_selected.Description.Length > 0)
			{
				GUILayout.Label(_selected.Description, RavenTheme.MutedLabel);
				RavenWidgets.Spacer(10f);
			}

			var loot = FeatureFactory.GetFeature<LootItems>();
			var trackName = _selected.ShortName.Length > 0 ? _selected.ShortName : _selected.Name;
			var tracked = loot != null && loot.TrackedNames.Any(t => t.Name.Equals(trackName, System.StringComparison.OrdinalIgnoreCase));

			var player = GameState.Current?.LocalPlayer;
			var inRaid = player.IsValid();

			GUILayout.BeginHorizontal();

			if (loot != null && RavenWidgets.OutlineButton(tracked ? "UNTRACK" : "TRACK", 90f))
			{
				if (tracked)
					loot.TrackedNames.RemoveAll(t => t.Name.Equals(trackName, System.StringComparison.OrdinalIgnoreCase));
				else
					loot.TrackedNames.Add(new TrackedItem(trackName));
			}

			GUILayout.Space(8f);

			if (inRaid && RavenWidgets.OutlineButton("SPAWN", 90f))
			{
				ConsoleCommands.Spawn.SpawnTemplate(_selected.Id, player!, new ConsoleCommands.Spawn(), _ => true);
				_status = $"Spawned {_selected.Name}.";
			}

			GUILayout.EndHorizontal();

			if (!inRaid)
			{
				RavenWidgets.Spacer(6f);
				GUILayout.Label("Spawning needs an active raid.", RavenTheme.MutedLabel);
			}

			if (_status.Length > 0)
			{
				RavenWidgets.Spacer(6f);
				GUILayout.Label(_status, RavenTheme.MutedLabel);
			}
		}
	}

	private void RefreshResults()
	{
		var query = _searchInput.Trim();
		if (_lastQuery != null && query == _lastQuery)
			return;

		_lastQuery = query;

		var entries = TemplateHelper.AllTemplates()
			.Select(t => new Entry
			{
				Name = t.NameLocalizationKey.Localized(),
				ShortName = t.ShortNameLocalizationKey.Localized(),
				Id = t.StringId,
				Template = t
			})
			.Where(e => e.Name.Length > 0 || e.ShortName.Length > 0);

		if (query.Length > 0)
		{
			entries = entries.Where(e =>
				e.Name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0
				|| e.ShortName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0
				|| e.Id.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0);
		}

		_results = [.. entries
			.GroupBy(e => e.Id, System.StringComparer.OrdinalIgnoreCase)
			.Select(g => g.First())
			.OrderBy(e => e.Name.Length > 0 ? e.Name : e.ShortName)];

		if (_selected != null && !_results.Any(e => e.Id == _selected.Id))
			_selected = null;

		RebuildCategories();
	}

	private void RebuildCategories()
	{
		var previous = _category > 0 && _category < _categories.Length ? _categories[_category] : null;

		_categories = ["All", .. _results
			.Select(e => e.Category)
			.Where(c => c.Length > 0)
			.Distinct(System.StringComparer.OrdinalIgnoreCase)
			.OrderBy(c => c)];

		_category = 0;

		if (previous == null)
			return;

		for (var i = 1; i < _categories.Length; i++)
		{
			if (!_categories[i].Equals(previous, System.StringComparison.OrdinalIgnoreCase))
				continue;

			_category = i;
			break;
		}
	}

	private Entry[] FilteredResults()
	{
		if (_categoryId.Length > 0)
			return [.. _results.Where(e => HandbookCatalog.IsUnder(e.Id, _categoryId))];

		if (_category <= 0 || _category >= _categories.Length)
			return _results;

		var wanted = _categories[_category];
		return [.. _results.Where(e => e.Category.Equals(wanted, System.StringComparison.OrdinalIgnoreCase))];
	}
}
