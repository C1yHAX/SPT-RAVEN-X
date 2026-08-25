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
	private readonly HashSet<BotsGroup> _added = [];
	private Player? _player;

	internal int GroupCount => _joined.Count;

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_player, player))
		{
			Restore();
			_player = player;
		}

		foreach (var hostile in state.Hostiles)
		{
			if (!hostile.IsAlive())
				continue;

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
			_added.Add(group);
		}
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
		if (_player != null)
		{
			foreach (var group in _added)
				group?.Allies?.Remove(_player);
		}

		_joined.Clear();
		_added.Clear();
		_player = null;
	}
}
