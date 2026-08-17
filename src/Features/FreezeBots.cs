using System.Collections.Generic;
using System.Linq;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class FreezeBots : ToggleFeature
{
	public override string Name => "freeze";
	public override string Description => "Suspend bot AI so nobody reacts or moves.";

	public override bool Enabled { get; set; } = false;

	private readonly Dictionary<BotOwner, EBotState> _suspended = [];

	internal int SuspendedCount => _suspended.Count;

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		if (state == null || !state.LocalPlayer.IsValid())
			return;

		foreach (var hostile in state.Hostiles.Where(h => h.IsAlive()))
		{
			var owner = hostile.AIData?.BotOwner;
			if (owner == null || _suspended.ContainsKey(owner))
				continue;

			if (owner.BotState == EBotState.NonActive || owner.BotState == EBotState.Disposed)
				continue;

			_suspended[owner] = owner.BotState;
			owner.BotState = EBotState.NonActive;
		}
	}

	protected override void UpdateWhenDisabled()
	{
		if (_suspended.Count == 0)
			return;

		foreach (var entry in _suspended.ToArray())
		{
			var owner = entry.Key;
			if (owner == null || owner.BotState == EBotState.Disposed)
				continue;

			owner.BotState = entry.Value;
		}

		_suspended.Clear();
	}
}
