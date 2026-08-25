using EFT.InventoryLogic;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Durability : ToggleFeature
{
	public override string Name => Strings.FeatureDurabilityName;
	public override string Description => Strings.FeatureDurabilityDescription;

	public override bool Enabled { get; set; } = false;

	private float _nextUpdate;

	protected override void UpdateWhenEnabled()
	{
		if (_nextUpdate > UnityEngine.Time.time)
			return;

		_nextUpdate = UnityEngine.Time.time + 0.25f;
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var allPlayerItems = player.Profile
			.Inventory
			.GetPlayerItems();

		foreach (var item in allPlayerItems)
		{
			var repairable = item?.GetItemComponent<RepairableComponent>();
			if (repairable == null)
				continue;

			repairable.MaxDurability = repairable.TemplateDurability;
			repairable.Durability = repairable.MaxDurability;
		}
	}

	protected override void UpdateWhenDisabled()
	{
		_nextUpdate = 0f;
	}
}
