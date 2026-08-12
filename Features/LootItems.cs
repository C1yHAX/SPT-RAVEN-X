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
	public bool TrackWishlist { get; set; } = false;

	[ConfigurationProperty]
	public bool TrackAutoWishlist { get; set; } = false;

	public override float CacheTimeInSec { get; set; } = 3f;
	public override Color GroupingColor => Color;

	public HashSet<string> Wishlist { get; set; } = [];

	public bool Track(string lootname, Color? color, ELootRarity? rarity)
	{
		lootname = lootname.Trim();

		if (TrackedNames.Any(t => t.Name == lootname && t.Rarity == rarity))
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
		Wishlist.Clear();
		Wishlist = RefreshWishlist();

		if (TrackedNames.Count == 0 && Wishlist.Count == 0)
			return;

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

			var position = valueTransform.position;
			FindItemsInRootItem(records, rootItem, position);
		}
	}

	private void FindItemsInRootItem(List<PointOfInterest> records, Item? rootItem, Vector3 position, string? ownerOverride = null)
	{
		var items = rootItem?
			.GetAllItems()?
			.ToArray();

		if (items == null)
			return;

		foreach (var item in items)
		{
			if (!item.IsValid() || item.IsFiltered())
				continue;

			TryAddRecordIfTracked(item, records, position, ownerOverride ?? item.Owner?.RootItem?.TemplateId.LocalizedShortName());
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
			FindItemsInRootItem(records, equipment, hostile.Transform.position, owner);
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

			TryAddRecordIfTracked(lootItem.Item, records, position);
		}
	}

	private string FormatName(string itemName, Item item)
	{
		var price = item.Template.CreditsPrice;
		if (!ShowPrices || price < 1000)
			return itemName;

		return $"{itemName} {price / 1000}K";
	}

	private void TryAddRecordIfTracked(Item item, List<PointOfInterest> records, Vector3 position, string? owner = null)
	{
		var itemName = item.ShortName.Localized();
		var template = item.Template;
		var templateId = template._id;
		var color = Color;

		if (!Wishlist.Contains(templateId))
		{
			var rarity = template.GetEstimatedRarity();
			var trackedItem = TryFindTrackedItem(itemName, templateId, rarity);
			if (trackedItem == null)
				return;

			color = trackedItem.Color ?? color;
		}

		if (owner != null && owner == KnownTemplateIds.DefaultInventoryLocalizedShortName)
			owner = nameof(Corpse);

		var poi = Pool.Get();
		poi.Name = FormatName(itemName, item);
		poi.Owner = string.Equals(itemName, owner, StringComparison.OrdinalIgnoreCase) ? null : owner;
		poi.Position = position;
		poi.Color = color;

		records.Add(poi);
	}

	private TrackedItem? TryFindTrackedItem(string itemName, string templateId, ELootRarity rarity)
	{
		return TrackedNames.FirstOrDefault(t => TextMatches(t, itemName, templateId) && RarityMatches(rarity, t.Rarity));
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
