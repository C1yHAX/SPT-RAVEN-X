using System.Linq;
using EFT.InventoryLogic;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class QuickTrow : TriggerFeature
{
	public override string Name => Strings.FeatureQuickTrowName;
	public override string Description => Strings.FeatureQuickTrowDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	protected override void UpdateOnceWhenTriggered()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var inventory = player
			.Profile
			.Inventory;

		var grenade = inventory
			.GetPlayerItems(EPlayerItems.Equipment)
			.OfType<ThrowWeap>()
			.FirstOrDefault();

		if (grenade == null)
			return;

		player.SetInHandsForQuickUse(grenade, null);
	}
}
