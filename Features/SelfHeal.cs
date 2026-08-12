using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class SelfHeal : TriggerFeature
{
	public override string Name => "heal";
	public override string Description => "Restore full health and remove negative effects once.";

	public override KeyCode Key { get; set; } = KeyCode.None;

	protected override void UpdateOnceWhenTriggered()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var healthController = player.ActiveHealthController;
		if (healthController == null)
			return;

		foreach (EBodyPart bodyPart in System.Enum.GetValues(typeof(EBodyPart)))
		{
			if (bodyPart == EBodyPart.Common)
				continue;

			if (healthController.IsBodyPartDestroyed(bodyPart))
				healthController.RestoreBodyPart(bodyPart, 1);
		}

		healthController.RestoreFullHealth();
	}
}
