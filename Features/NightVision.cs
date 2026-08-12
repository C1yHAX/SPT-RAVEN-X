using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NightVision : ToggleFeature
{
	public override string Name => Strings.FeatureNightVisionName;
	public override string Description => Strings.FeatureNightVisionDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool FullScreen { get; set; } = true;

	[ConfigurationProperty(Order = 11)]
	public float Intensity { get; set; } = 0.4f;

	[ConfigurationProperty(Order = 12)]
	public float Noise { get; set; } = 0f;

	[ConfigurationProperty(Order = 13)]
	public Color Tint { get; set; } = new(0.16f, 1f, 0.32f);

	private Texture? _originalMask;
	private float _originalMaskSize;
	private float _originalIntensity;
	private float _originalNoise;
	private Color _originalColor;
	private bool _captured;
	private bool _applied;

	protected override void Update()
	{
		base.Update();

		var player = GameState.Current?.LocalPlayer;
		if (player == null || player is HideoutPlayer || player.HasItemComponentInSlot<NightVisionComponent>(EquipmentSlot.Headwear))
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		var component = camera.GetComponent<BSG.CameraEffects.NightVision>();
		if (component == null)
			return;

		if (!_captured)
		{
			_originalMask = component.Mask;
			_originalMaskSize = component.MaskSize;
			_originalIntensity = component.Intensity;
			_originalNoise = component.NoiseIntensity;
			_originalColor = component.Color;
			_captured = true;
		}

		if (component.On != Enabled)
			component.StartSwitch(Enabled);

		if (!Enabled)
		{
			Restore(component);
			return;
		}

		if (FullScreen)
		{
			component.Mask = null;
			component.MaskSize = 1f;

			var overlay = component.TextureMask;
			if (overlay != null)
			{
				overlay.Color = new Color(0f, 0f, 0f, 0f);
				overlay.Stretch = true;
				overlay.Size = 0f;
			}
		}

		component.Intensity = Intensity;
		component.NoiseIntensity = Noise;
		component.Color = Tint;

		_applied = true;
	}

	private void Restore(BSG.CameraEffects.NightVision component)
	{
		if (!_applied)
			return;

		component.Mask = _originalMask;
		component.MaskSize = _originalMaskSize;
		component.Intensity = _originalIntensity;
		component.NoiseIntensity = _originalNoise;
		component.Color = _originalColor;

		_applied = false;
	}
}
