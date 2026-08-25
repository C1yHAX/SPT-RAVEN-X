using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JsonType;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class LootItems : PointOfInterests
{
	public override string Name => Strings.FeatureLootItemsName;
	public override string Description => Strings.FeatureLootItemsDescription;

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.cyan;

	[ConfigurationProperty(Browsable = false, CommentResourceId = nameof(Strings.PropertyTrackedNamesComment))]
	public List<TrackedItem> TrackedNames { get; set; } = [];

	[ConfigurationProperty]
	public bool SearchInsideContainers { get; set; } = true;

	[ConfigurationProperty]
	public bool SearchInsideCorpses { get; set; } = true;

	[ConfigurationProperty]
	public bool SearchInsideLivingAI { get; set; } = false;

	[ConfigurationProperty]
	public bool ShowPrices { get; set; } = true;

	[ConfigurationProperty]
	public int MinimumPrice { get; set; } = 0;

	[ConfigurationProperty]
	public int MaximumPrice { get; set; } = 0;

	[ConfigurationProperty]
	public ELootRarity MinimumRarity { get; set; } = ELootRarity.Not_exist;

	[ConfigurationProperty]
	public bool TrackWishlist { get; set; } = false;

	[ConfigurationProperty]
	public bool TrackAutoWishlist { get; set; } = false;

	public override float CacheTimeInSec { get; set; } = 3f;
	public override Color GroupingColor => Color;

	public HashSet<string> Wishlist { get; set; } = [];

	public bool Track(string lootname, Color? color, ELootRarity? rarity)
	{
		lootname = lootname.Trim();

		if (TrackedNames.Any(t => string.Equals(t.Name, lootname, StringComparison.OrdinalIgnoreCase) && t.Rarity == rarity))
			return false;

		TrackedNames.Add(new TrackedItem(lootname, color, rarity));
		return true;

	}

	public bool UnTrack(string lootname)
	{
		lootname = lootname.Trim();

		if (lootname == TrackedItem.MatchAll && TrackedNames.Count > 0)
		{
			TrackedNames.Clear();
			return true;
		}

		return TrackedNames.RemoveAll(t => t.Name == lootname) > 0;
	}

	private HashSet<string> RefreshWishlist()
	{
		if (!TrackWishlist && !TrackAutoWishlist)
			return [];

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return [];

		var manager = player.Profile?.WishlistManager;
		if (manager == null)
			return [];

		return TrackWishlist switch
		{
			true when TrackAutoWishlist => [.. manager.GetWishlist().Keys],
			true when !TrackAutoWishlist => [.. manager.UserItems.Keys],
			false when TrackAutoWishlist => [.. manager.GetWishlist().Keys.Except(manager.UserItems.Keys)],
			_ => []
		};
	}

	public override void RefreshData(List<PointOfInterest> data)
	{
		Wishlist = RefreshWishlist();

		HandbookCatalog.Refresh();

		var world = Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		FindLootItems(world, data);

		if (SearchInsideContainers)
			FindItemsInContainers(world, data);

		if (SearchInsideLivingAI)
			FindItemsOnLivingAI(data);
	}

	private void FindItemsInContainers(GameWorld world, List<PointOfInterest> records)
	{
		var owners = world.ItemOwners;
		foreach (var (key, ownerValue) in owners)
		{
			var rootItem = key.RootItem;
			if (rootItem is not { IsContainer: true })
				continue;

			if (!rootItem.IsValid() || rootItem.IsFiltered())
				continue;

			var valueTransform = ownerValue.Transform;
			if (valueTransform == null)
				continue;

			if (valueTransform.GetComponentInParent<Player>() != null)
				continue;

			var position = valueTransform.position;
			FindItemsInRootItem(records, rootItem, position);
		}
	}

	private void FindItemsInRootItem(List<PointOfInterest> records, Item? rootItem, Vector3 position, string? ownerOverride = null, Transform? follow = null)
	{
		var items = rootItem?.GetAllItems();

		if (items == null)
			return;

		foreach (var item in items)
		{
			if (!item.IsValid() || item.IsFiltered())
				continue;

			TryAddRecord(item, records, position, ownerOverride ?? item.Owner?.RootItem?.TemplateId.LocalizedShortName(), follow);
		}
	}

	private void FindItemsOnLivingAI(List<PointOfInterest> records)
	{
		var state = GameState.Current;
		if (state == null)
			return;

		foreach (var hostile in state.Hostiles)
		{
			if (hostile == null || !hostile.IsAlive() || hostile.IsYourPlayer)
				continue;

			var equipment = hostile.InventoryController?.Inventory?.Equipment;
			if (equipment == null)
				continue;

			var owner = hostile.Profile?.Info?.Settings?.Role.ToString() ?? hostile.GetHostileType().ToString();
			FindItemsInRootItem(records, equipment, hostile.Transform.position, owner, hostile.gameObject.transform);
		}
	}

	private void FindLootItems(GameWorld world, List<PointOfInterest> records)
	{
		var lootItems = world.LootItems;

		for (var i = 0; i < lootItems.Count; i++)
		{
			var lootItem = lootItems.GetByIndex(i);
			if (!lootItem.IsValid())
				continue;

			var position = lootItem.transform.position;

			if (lootItem is Corpse corpse)
			{
				if (SearchInsideCorpses)
					FindItemsInRootItem(records, corpse.ItemOwner?.RootItem, position);

				continue;
			}

			TryAddRecord(lootItem.Item, records, position);
		}
	}

	internal static int PriceOf(ItemTemplate template)
	{
		var handbook = HandbookCatalog.PriceOf(template._id);
		if (handbook > 0f)
			return Mathf.RoundToInt(handbook);

		return template.CreditsPrice;
	}

	private static int PriceOf(Item item)
	{
		var unitPrice = PriceOf(item.Template);
		if (unitPrice <= 0)
			return 0;

		var total = (long)unitPrice * Mathf.Max(1, item.StackObjectsCount);
		return total >= int.MaxValue ? int.MaxValue : (int)total;
	}

	private string FormatName(string itemName, Item item)
	{
		if (!ShowPrices)
			return itemName;

		var price = PriceOf(item);
		if (price <= 0)
			return itemName;

		return price >= 1000
			? $"{itemName} {price / 1000f:0.#}K ₽"
			: $"{itemName} {price} ₽";
	}

	private void TryAddRecord(Item item, List<PointOfInterest> records, Vector3 position, string? owner = null, Transform? follow = null)
	{
		if (IsOutOfRange(position))
			return;

		var itemName = item.ShortName.Localized();
		var template = item.Template;
		var templateId = template._id;
		var color = Color;

		var rarity = template.GetEstimatedRarity();
		var trackedItem = TryFindTrackedItem(itemName, templateId, rarity);

		if (trackedItem?.Color != null)
			color = trackedItem.Color.Value;

		if (!Wishlist.Contains(templateId) && !PassesFilters(item, rarity))
			return;

		if (owner != null && owner == KnownTemplateIds.DefaultInventoryLocalizedShortName)
			owner = nameof(Corpse);

		var poi = Pool.Get();
		poi.Name = FormatName(itemName, item);
		poi.Owner = string.Equals(itemName, owner, StringComparison.OrdinalIgnoreCase) ? null : owner;
		poi.Position = position;
		poi.Color = color;

		poi.Follow = follow;

		records.Add(poi);
	}

	private bool IsOutOfRange(Vector3 position)
	{
		if (MaximumDistance <= 0)
			return false;

		var snapshot = GameState.Current;

		if (snapshot == null || snapshot.MapMode)
			return false;

		var camera = snapshot.Camera;
		if (camera == null)
			return false;

		var limit = MaximumDistance;
		return (position - camera.transform.position).sqrMagnitude > limit * limit;
	}

	private bool PassesFilters(Item item, ELootRarity rarity)
	{
		var lower = MinimumPrice;
		var upper = MaximumPrice;

		if (upper > 0 && lower > upper)
			(lower, upper) = (upper, lower);

		if (lower > 0 || upper > 0)
		{
			var price = PriceOf(item);
			if (price <= 0)
				return false;

			if (lower > 0 && price < lower)
				return false;

			if (upper > 0 && price > upper)
				return false;
		}

		return RarityRank(rarity) >= RarityRank(MinimumRarity);
	}

	public static int RarityRank(ELootRarity rarity) => rarity switch
	{
		ELootRarity.Superrare => 3,
		ELootRarity.Rare => 2,
		ELootRarity.Common => 1,
		_ => 0
	};

	private TrackedItem? TryFindTrackedItem(string itemName, string templateId, ELootRarity rarity)
	{
		for (var i = 0; i < TrackedNames.Count; i++)
		{
			var tracked = TrackedNames[i];
			if (TextMatches(tracked, itemName, templateId) && RarityMatches(rarity, tracked.Rarity))
				return tracked;
		}

		return null;
	}

	private static bool TextMatches(TrackedItem trackedItem, string itemName, string templateId)
	{
		return trackedItem.IsMatchAll
			   || itemName.IndexOf(trackedItem.Name, StringComparison.OrdinalIgnoreCase) >= 0
			   || string.Equals(templateId, trackedItem.Name, StringComparison.OrdinalIgnoreCase);
	}

	private static bool RarityMatches(ELootRarity itemRarity, ELootRarity? trackedRarity)
	{
		if (!trackedRarity.HasValue)
			return true;

		return trackedRarity.Value == itemRarity;
	}
}
