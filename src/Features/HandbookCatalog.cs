using System.Collections.Generic;
using EFT.HandBook;
using RavenX.Extensions;
using EFT.UI;
using EFT;

#nullable enable

namespace RavenX.Features;

internal sealed class CatalogCategory
{
	public string Id = string.Empty;
	public string ParentId = string.Empty;
	public string Name = string.Empty;
	public int Order;
	public int Depth;
	public bool Expanded;
	public readonly List<CatalogCategory> Children = [];
}

internal static class HandbookCatalog
{
	private static Handbook? _source;
	private static int _sourceCategoryCount = -1;
	private static int _sourceItemCount = -1;
	private static bool _hasPositivePrice;

	private static readonly List<CatalogCategory> _roots = [];
	private static readonly Dictionary<string, CatalogCategory> _byId = new();
	private static readonly Dictionary<string, string> _categoryOfItem = new();
	private static readonly Dictionary<string, string> _parentOf = new();

	private static CatalogCategory? NamedAncestor(string parentId)
	{
		for (var depth = 0; depth < 16 && parentId.Length > 0; depth++)
		{
			if (_byId.TryGetValue(parentId, out var found))
				return found;

			if (!_parentOf.TryGetValue(parentId, out parentId))
				return null;
		}

		return null;
	}
	private static readonly Dictionary<string, float> _priceOfItem = new();

	public static IReadOnlyList<CatalogCategory> Roots => _roots;
	public static bool Ready => _roots.Count > 0 && _categoryOfItem.Count > 0;
	public static bool PricesReady => _hasPositivePrice;

	public static void Refresh()
	{
		var handbook = ItemUiContext.Instance?.Handbook;
		if (handbook == null)
			return;

		var categories = handbook.Categories;
		var items = handbook.Items;
		var categoryCount = CountEntries(categories);
		var itemCount = CountEntries(items);
		var sameSource = ReferenceEquals(handbook, _source);
		var rebuildCategories = !sameSource || categoryCount != _sourceCategoryCount;
		var rebuildItems = !sameSource || itemCount != _sourceItemCount || !_hasPositivePrice;

		if (!rebuildCategories && !rebuildItems)
			return;

		_source = handbook;
		_sourceCategoryCount = categoryCount;
		_sourceItemCount = itemCount;

		if (rebuildCategories)
		{
			var expanded = new HashSet<string>();
			foreach (var category in _byId.Values)
			{
				if (category.Expanded)
					expanded.Add(category.Id);
			}

			_roots.Clear();
			_byId.Clear();
			_parentOf.Clear();

			if (categories != null)
			{
				foreach (var data in categories)
				{
					if (data == null || string.IsNullOrEmpty(data.Id))
						continue;

					_parentOf[data.Id] = data.ParentId ?? string.Empty;

					var name = Describe(data);
					if (name == null)
						continue;

					_byId[data.Id] = new CatalogCategory
					{
						Id = data.Id,
						ParentId = data.ParentId ?? string.Empty,
						Name = name,
						Order = data.Order,
						Expanded = expanded.Contains(data.Id)
					};
				}

				foreach (var category in _byId.Values)
				{
					var parent = NamedAncestor(category.ParentId);

					if (parent != null)
						parent.Children.Add(category);
					else
						_roots.Add(category);
				}

				foreach (var root in _roots)
					Depths(root, 0);

				Sort(_roots);
			}
		}

		if (!rebuildItems)
			return;

		_categoryOfItem.Clear();
		_priceOfItem.Clear();
		_hasPositivePrice = false;

		if (items == null)
			return;

		foreach (var data in items)
		{
			if (data == null || string.IsNullOrEmpty(data.Id))
				continue;

			if (!string.IsNullOrEmpty(data.ParentId))
				_categoryOfItem[data.Id] = data.ParentId;

			_priceOfItem[data.Id] = data.Price;
			if (data.Price > 0f)
				_hasPositivePrice = true;
		}
	}

	private static int CountEntries<T>(IEnumerable<T>? entries)
	{
		if (entries == null)
			return 0;

		if (entries is ICollection<T> collection)
			return collection.Count;

		if (entries is IReadOnlyCollection<T> readOnly)
			return readOnly.Count;

		var count = 0;
		foreach (var _ in entries)
			count++;

		return count;
	}

	private static void Depths(CatalogCategory category, int depth)
	{
		category.Depth = depth;

		foreach (var child in category.Children)
			Depths(child, depth + 1);
	}

	private static void Sort(List<CatalogCategory> categories)
	{
		categories.Sort((left, right) =>
		{
			var order = left.Order.CompareTo(right.Order);
			return order != 0 ? order : string.CompareOrdinal(left.Name, right.Name);
		});

		foreach (var category in categories)
			Sort(category.Children);
	}

	private static string? Describe(HandbookData data)
	{
		if (TryLocalize(data.Id, out var resolved))
			return resolved;

		if (TryLocalize(data.Id + " Name", out resolved))
			return resolved;

		if (!string.IsNullOrEmpty(data.Name))
		{
			if (TryLocalize(data.Name, out resolved))
				return resolved;

			return data.Name;
		}

		return null;
	}

	private static bool TryLocalize(string key, out string resolved)
	{
		resolved = key.Localized();
		return resolved.Length > 0 && resolved != key;
	}

	public static string PathOf(string templateId)
	{
		if (!_categoryOfItem.TryGetValue(templateId, out var current))
			return string.Empty;

		var chain = new List<string>();

		for (var depth = 0; depth < 16 && current.Length > 0; depth++)
		{
			if (_byId.TryGetValue(current, out var category))
				chain.Insert(0, category.Name);

			if (!_parentOf.TryGetValue(current, out current))
				break;
		}

		return string.Join("  ›  ", chain.ToArray());
	}

	public static float PriceOf(string templateId) => _priceOfItem.TryGetValue(templateId, out var price) ? price : 0f;

	public static bool IsUnder(string templateId, string categoryId)
	{
		if (!_categoryOfItem.TryGetValue(templateId, out var current))
			return false;

		for (var depth = 0; depth < 16 && current.Length > 0; depth++)
		{
			if (current == categoryId)
				return true;

			if (!_parentOf.TryGetValue(current, out current))
				return false;
		}

		return false;
	}

	public static void Flatten(List<CatalogCategory> target)
	{
		target.Clear();

		foreach (var root in _roots)
			Flatten(root, target);
	}

	private static void Flatten(CatalogCategory category, List<CatalogCategory> target)
	{
		target.Add(category);

		if (!category.Expanded)
			return;

		foreach (var child in category.Children)
			Flatten(child, target);
	}
}
