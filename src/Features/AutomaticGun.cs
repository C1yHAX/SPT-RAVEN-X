using System.Collections.Generic;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class AutomaticGun : ToggleFeature
{
	public override string Name => Strings.FeatureAutomaticGunName;
	public override string Description => Strings.FeatureAutomaticGunDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool OverrideRate { get; set; } = false;

	[ConfigurationProperty(Order = 11)]
	public int Rate { get; set; } = 500;

	private readonly Dictionary<WeaponTemplate, KeyValuePair<int, bool>> _originals = [];

	private bool _rateApplied;
	private bool _autoApplied;

	protected override void Update()
	{
		base.Update();

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player!.HandsController?.Item is not Weapon weapon)
			return;

		if (weapon.Template is not WeaponTemplate template)
			return;

		if (!_originals.ContainsKey(template))
			_originals[template] = new KeyValuePair<int, bool>(template.bFirerate, template.BoltAction);

		if (Enabled)
		{
			var fireModeComponent = weapon.GetItemComponent<FireModeComponent>();
			if (fireModeComponent != null)
				fireModeComponent.FireMode = Weapon.EFireMode.fullauto;

			template.BoltAction = false;
			_autoApplied = true;
		}
		else if (_autoApplied)
		{
			foreach (var entry in _originals)
				entry.Key.BoltAction = entry.Value.Value;

			_autoApplied = false;
		}

		if (OverrideRate)
		{
			template.bFirerate = Rate;
			_rateApplied = true;
			return;
		}

		if (!_rateApplied)
			return;

		foreach (var entry in _originals)
			entry.Key.bFirerate = entry.Value.Key;

		_rateApplied = false;
	}
}
