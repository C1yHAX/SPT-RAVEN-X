using System.Linq;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class KillAllBots : TriggerFeature
{
	public override string Name => "killall";
	public override string Description => "Kill every living bot in the raid.";

	public override KeyCode Key { get; set; } = KeyCode.None;

	internal int LastKilledCount { get; private set; }

	protected override void UpdateOnceWhenTriggered()
	{
		LastKilledCount = 0;

		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
			return;

		foreach (var hostile in state.Hostiles)
		{
			if (!hostile.IsAlive() || hostile.IsYourPlayer || hostile.AIData?.BotOwner == null)
				continue;

			var health = hostile.ActiveHealthController;
			if (health == null)
				continue;

			health.Kill(EDamageType.Undefined);
			LastKilledCount++;
		}
	}
}
