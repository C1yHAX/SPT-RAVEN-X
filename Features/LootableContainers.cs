using System.Collections.Generic;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class LootableContainers : PointOfInterests
{
	public override string Name => Strings.FeatureStashName;
	public override string Description => Strings.FeatureStashDescription;

	private static readonly HashSet<string> _targetedContainer =
	[
		KnownTemplateIds.BuriedBarrelCache,
		KnownTemplateIds.GroundCache,
		KnownTemplateIds.AirDropCommon,
		KnownTemplateIds.AirDropMedical,
		KnownTemplateIds.AirDropSupply,
		KnownTemplateIds.AirDropWeapon
	];

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.white;

	public override float CacheTimeInSec { get; set; } = 11f;
	public override bool Enabled { get; set; } = false;
	public override Color GroupingColor => Color;

	[ConfigurationProperty]
	public bool ShowContainers { get; set; } = true;

	[ConfigurationProperty]
	public bool ShowCorpses { get; set; } = true;

	[ConfigurationProperty]
	public Color CorpseColor { get; set; } = new(0.85f, 0.35f, 0.35f);

	public override void RefreshData(List<PointOfInterest> data)
	{
		var world = Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		var owners = world.ItemOwners;

		foreach (var owner in owners)
		{
			var itemOwner = owner.Key;
			var rootItem = itemOwner.RootItem;

			if (!rootItem.IsValid())
				continue;

			if (rootItem is not { IsContainer: true })
				continue;

			if (ShowContainers && _targetedContainer.Contains(rootItem.TemplateId))
				AddRecord(rootItem.TemplateId.LocalizedShortName(), owner.Value.Transform.position, Color, data);
		}

		if (!ShowCorpses)
			return;

		var lootItems = world.LootItems;

		for (var i = 0; i < lootItems.Count; i++)
		{
			var lootItem = lootItems.GetByIndex(i);
			if (!lootItem.IsValid())
				continue;

			if (lootItem is Corpse)
				AddRecord("Body", lootItem.transform.position, CorpseColor, data);
		}
	}

	private static void AddRecord(string itemName, Vector3 position, Color color, List<PointOfInterest> records)
	{
		var poi = Pool.Get();
		poi.Name = itemName;
		poi.Position = position;
		poi.Color = color;
		poi.Owner = null;

		records.Add(poi);
	}
}
