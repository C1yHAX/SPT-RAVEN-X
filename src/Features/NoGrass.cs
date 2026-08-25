using System.Collections.Generic;
using GPUInstancer;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoGrass : ToggleFeature
{
	public override string Name => "nograss";
	public override string Description => "Stop the ground detail meshes from rendering.";

	public override bool Enabled { get; set; } = false;

	private const float ScanIntervalInSec = 2f;

	private readonly List<GPUInstancerDetailManager> _suppressed = [];
	private float _nextScan;

	protected override void UpdateWhenEnabled()
	{
		if (Time.time < _nextScan)
			return;

		_nextScan = Time.time + ScanIntervalInSec;

		var managers = FindObjectsOfType<GPUInstancerDetailManager>();
		if (managers == null)
			return;

		foreach (var manager in managers)
		{
			if (manager == null || !manager.enabled)
				continue;

			manager.enabled = false;
			_suppressed.Add(manager);
		}
	}

	protected override void UpdateWhenDisabled()
	{
		if (_suppressed.Count == 0)
			return;

		foreach (var manager in _suppressed)
		{
			if (manager != null)
				manager.enabled = true;
		}

		_suppressed.Clear();
		_nextScan = 0f;
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		UpdateWhenDisabled();
	}
}
