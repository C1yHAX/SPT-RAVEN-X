using Comfort.Common;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Mortar : TriggerFeature
{
	public override string Name => Strings.FeatureMortarName;
	public override string Description => Strings.FeatureMortarDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	protected override void UpdateOnceWhenTriggered()
	{
		var world = Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		world.ServerShellingController?.StartShellingPosition(player.Transform.position);
	}
}
