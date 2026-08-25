using System;
using EFT.InventoryLogic;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class ThermalVision : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureThermalVisionName;
	public override string Description => Properties.Strings.FeatureThermalVisionDescription;

	public override bool Enabled { get; set; } = false;

	private global::ThermalVision? _component;
	private Action? _restore;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null || player is HideoutPlayer || player.HasItemComponentInSlot<ThermalVisionComponent>(EquipmentSlot.Headwear))
		{
			Restore();
			return;
		}

		var component = GameState.Current?.Camera?.GetComponent<global::ThermalVision>();
		if (component == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_component, component))
			Capture(component);

		if (!component.On)
			component.StartSwitch(true);

		component.IsFpsStuck = false;
		component.IsGlitch = false;
		component.IsMotionBlurred = false;
		component.IsNoisy = false;
		component.IsPixelated = false;

		var overlay = component.TextureMask;
		if (overlay != null)
		{
			overlay.Color = new Color(0f, 0f, 0f, 0f);
			overlay.Stretch = false;
			overlay.Size = 0f;
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

	private void Capture(global::ThermalVision component)
	{
		Restore();
		var on = component.On;
		var fpsStuck = component.IsFpsStuck;
		var glitch = component.IsGlitch;
		var motionBlurred = component.IsMotionBlurred;
		var noisy = component.IsNoisy;
		var pixelated = component.IsPixelated;
		var overlay = component.TextureMask;
		var overlayColor = overlay?.Color ?? default;
		var overlayStretch = overlay?.Stretch ?? false;
		var overlaySize = overlay?.Size ?? 0f;

		_component = component;
		_restore = () =>
		{
			if (component == null)
				return;

			if (component.On != on)
				component.StartSwitch(on);
			component.IsFpsStuck = fpsStuck;
			component.IsGlitch = glitch;
			component.IsMotionBlurred = motionBlurred;
			component.IsNoisy = noisy;
			component.IsPixelated = pixelated;
			if (overlay == null)
				return;

			overlay.Color = overlayColor;
			overlay.Stretch = overlayStretch;
			overlay.Size = overlaySize;
		};
	}

	private void Restore()
	{
		_restore?.Invoke();
		_component = null;
		_restore = null;
	}
}
