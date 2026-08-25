using System;
using RavenX.Configuration;
using JetBrains.Annotations;
using UnityEngine;

namespace RavenX.Features;

[UsedImplicitly]
internal class FovChanger : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureFovChangerName;
	public override string Description => Properties.Strings.FeatureFovChangerDescription;

	[ConfigurationProperty(Order = 1)]
	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 2)]
	public float Fov { get; set; } = 75f;

	[ConfigurationProperty(Order = 3)]
	public float CameraOffset { get; set; } = 0.05f;

	private Camera? _camera;
	private object? _container;
	private Action? _restore;

	[UsedImplicitly]
	private void LateUpdate()
	{
		if (!Enabled)
			return;

		var snapshot = GameState.Current;
		var camera = snapshot?.Camera;
		var player = snapshot?.LocalPlayer;
		var container = player?.ProceduralWeaponAnimation?.HandsContainer;
		if (camera == null || container == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_camera, camera) || !ReferenceEquals(_container, container))
		{
			Restore();
			var fieldOfView = camera.fieldOfView;
			var cameraOffset = container.CameraOffset;
			_camera = camera;
			_container = container;
			_restore = () =>
			{
				if (camera != null)
					camera.fieldOfView = fieldOfView;
				container.CameraOffset = cameraOffset;
			};
		}

		container.CameraOffset = new Vector3(0.04f, 0.04f, CameraOffset);
		camera.fieldOfView = Mathf.Clamp(Fov, 1f, 179f);
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
		_restore?.Invoke();
		_camera = null;
		_container = null;
		_restore = null;
	}
}
