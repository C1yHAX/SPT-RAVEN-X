using System.Linq;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class GatherBots : TriggerFeature
{
	public override string Name => "gather";
	public override string Description => "Teleport all living bots to your position.";

	public override KeyCode Key { get; set; } = KeyCode.None;

	internal int LastMovedCount { get; private set; }

	protected override void UpdateOnceWhenTriggered()
	{
		LastMovedCount = 0;

		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
			return;

		var origin = player.Transform.position;
		var forward = player.Transform.forward;
		var hostiles = state.Hostiles.Where(h => h.IsAlive()).ToArray();

		for (var i = 0; i < hostiles.Length; i++)
		{

			var angle = i / (float)Mathf.Max(1, hostiles.Length) * Mathf.PI * 2f;
			var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3f;

			hostiles[i].Teleport(origin + forward * 3f + offset, false);
			LastMovedCount++;
		}
	}
}
