using RavenX.Configuration;
using JetBrains.Annotations;
using EFT;

namespace RavenX.Features;

[UsedImplicitly]
internal class Interact : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureInteractName;
	public override string Description => Properties.Strings.FeatureInteractDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty]
	public float Distance { get; set; } = 1f;

	private bool _applied;
	private float _lootDistance;
	private float _doorDistance;

	protected override void UpdateWhenEnabled()
	{
		var settings = EFTHardSettings.Instance;
		if (!_applied)
		{
			_lootDistance = settings.LOOT_RAYCAST_DISTANCE;
			_doorDistance = settings.DOOR_RAYCAST_DISTANCE;
			_applied = true;
		}

		var distance = UnityEngine.Mathf.Max(0.1f, Distance);
		settings.LOOT_RAYCAST_DISTANCE = distance;
		settings.DOOR_RAYCAST_DISTANCE = distance;
	}

	protected override void UpdateWhenDisabled()
	{
		Restore();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		Restore();
	}

	private void Restore()
	{
		if (!_applied)
			return;

		var settings = EFTHardSettings.Instance;
		settings.LOOT_RAYCAST_DISTANCE = _lootDistance;
		settings.DOOR_RAYCAST_DISTANCE = _doorDistance;
		_applied = false;
	}
}
