using System;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoVisor : ToggleFeature
{
	public override string Name => Strings.FeatureNoVisorName;
	public override string Description => Strings.FeatureNoVisorDescription;

	public override bool Enabled { get; set; } = false;

	protected override void Update()
	{
		base.Update();

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		var component = camera.GetComponent<VisorEffect>();
		if (component == null || Mathf.Abs(component.Intensity - Convert.ToInt32(!Enabled)) < Mathf.Epsilon)
			return;

		component.Intensity = Convert.ToInt32(!Enabled);
	}
}
