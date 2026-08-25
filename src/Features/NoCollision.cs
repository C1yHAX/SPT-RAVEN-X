using System.Collections.Generic;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoCollision : ToggleFeature
{
	public override string Name => Strings.FeatureNoCollisionName;
	public override string Description => Strings.FeatureNoCollisionDescription;

	public override bool Enabled { get; set; } = false;

	private readonly Dictionary<Rigidbody, bool> _originals = [];
	private Player? _player;
	private float _nextScan;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_player, player))
		{
			Restore();
			_player = player;
		}

		if (_nextScan > Time.unscaledTime)
			return;

		_nextScan = Time.unscaledTime + 0.25f;
		foreach (var rigidbody in player.GetComponentsInChildren<Rigidbody>(true))
		{
			if (!_originals.ContainsKey(rigidbody))
				_originals.Add(rigidbody, rigidbody.detectCollisions);

			rigidbody.detectCollisions = false;
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
		foreach (var pair in _originals)
		{
			if (pair.Key != null)
				pair.Key.detectCollisions = pair.Value;
		}

		_originals.Clear();
		_player = null;
		_nextScan = 0f;
	}
}
