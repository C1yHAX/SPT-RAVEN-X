using EFT.Interactive;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class WorldInteractiveObjects : TriggerFeature
{
	public override string Name => Strings.FeatureWorldInteractiveObjectsName;
	public override string Description => Strings.FeatureWorldInteractiveObjectsDescription;

	public override KeyCode Key { get; set; } = KeyCode.KeypadPeriod;

	protected override void UpdateOnceWhenTriggered()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var objects = LocationScene.GetAllObjects<WorldInteractiveObject>();
		foreach (var obj in objects)
		{
			if (!obj.IsValid())
				continue;

			if (obj.DoorState != EDoorState.Locked)
				continue;

			var offset = player.Transform.position - obj.transform.position;
			var sqrLen = offset.sqrMagnitude;

			if (sqrLen <= 20.0f)
				obj.DoorState = EDoorState.Shut;
		}
	}
}
