using System.Collections.Generic;
using System.Linq;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class FriendlyBots : ToggleFeature
{
	public override string Name => "friendly";
	public override string Description => "Bots treat you as an ally instead of a target.";

	public override bool Enabled { get; set; } = false;

	private readonly HashSet<BotsGroup> _joined = [];

	internal int GroupCount => _joined.Count;

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
			return;

		foreach (var hostile in state.Hostiles.Where(h => h.IsAlive()))
		{
			var group = hostile.AIData?.BotOwner?.BotsGroup;
			if (group == null || _joined.Contains(group))
				continue;

			if (group.IsAlly(player))
			{
				_joined.Add(group);
				continue;
			}

			group.AddAlly(player);
			_joined.Add(group);
		}
	}

	protected override void UpdateWhenDisabled()
	{
		_joined.Clear();
	}
}
