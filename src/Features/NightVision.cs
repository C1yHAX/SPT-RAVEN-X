using System;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NightVision : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureNightVisionName;
	public override string Description => Properties.Strings.FeatureNightVisionDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool FullScreen { get; set; } = true;

	[ConfigurationProperty(Order = 11)]
	public float Intensity { get; set; } = 0.4f;

	[ConfigurationProperty(Order = 12)]
	public float Noise { get; set; } = 0f;

	[ConfigurationProperty(Order = 13)]
	public Color Tint { get; set; } = new(0.16f, 1f, 0.32f);

	private BSG.CameraEffects.NightVision? _component;
	private Action? _restore;
	private Action<bool>? _applyMask;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null || player is HideoutPlayer || player.HasItemComponentInSlot<NightVisionComponent>(EquipmentSlot.Headwear))
		{
			Restore();
			return;
		}

		var component = GameState.Current?.Camera?.GetComponent<BSG.CameraEffects.NightVision>();
		if (component == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_component, component))
			Capture(component);

		if (!component.On)
			component.StartSwitch(true);

		_applyMask?.Invoke(FullScreen);
		component.Intensity = Mathf.Max(0f, Intensity);
		component.NoiseIntensity = Mathf.Max(0f, Noise);
		component.Color = Tint;
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

	private void Capture(BSG.CameraEffects.NightVision component)
	{
		Restore();
		var on = component.On;
		var mask = component.Mask;
		var maskSize = component.MaskSize;
		var intensity = component.Intensity;
		var noise = component.NoiseIntensity;
		var color = component.Color;
		var overlay = component.TextureMask;
		var overlayColor = overlay?.Color ?? default;
		var overlayStretch = overlay?.Stretch ?? false;
		var overlaySize = overlay?.Size ?? 0f;

		_component = component;
		_applyMask = fullScreen =>
		{
			component.Mask = fullScreen ? null : mask;
			component.MaskSize = fullScreen ? 1f : maskSize;
			if (overlay == null)
				return;

			overlay.Color = fullScreen ? new Color(0f, 0f, 0f, 0f) : overlayColor;
			overlay.Stretch = fullScreen || overlayStretch;
			overlay.Size = fullScreen ? 0f : overlaySize;
		};
		_restore = () =>
		{
			if (component == null)
				return;

			if (component.On != on)
				component.StartSwitch(on);
			component.Mask = mask;
			component.MaskSize = maskSize;
			component.Intensity = intensity;
			component.NoiseIntensity = noise;
			component.Color = color;
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
		_applyMask = null;
	}
}
