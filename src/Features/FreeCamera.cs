using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class FreeCamera : ToggleFeature
{
	private const string MouseXAxis = "Mouse X";
	private const string MouseYAxis = "Mouse Y";

	public override string Name => Strings.FeatureCameraName;
	public override string Description => Strings.FeatureCameraDescription;

	[ConfigurationProperty(Skip = true)]
	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 20)]
	public KeyCode Forward { get; set; } = KeyCode.UpArrow;

	[ConfigurationProperty(Order = 21)]
	public KeyCode Backward { get; set; } = KeyCode.DownArrow;

	[ConfigurationProperty(Order = 22)]
	public KeyCode Left { get; set; } = KeyCode.LeftArrow;

	[ConfigurationProperty(Order = 23)]
	public KeyCode Right { get; set; } = KeyCode.RightArrow;

	[ConfigurationProperty(Order = 24)]
	public KeyCode FastMode { get; set; } = KeyCode.RightShift;

	[ConfigurationProperty(Order = 25)]
	public KeyCode Teleport { get; set; } = KeyCode.T;

	[ConfigurationProperty(Order = 30)]
	public float FreeLookSensitivity { get; set; } = 3f;

	[ConfigurationProperty(Order = 31)]
	public float MovementSpeed { get; set; } = 10f;

	[ConfigurationProperty(Order = 32)]
	public float FastMovementSpeed { get; set; } = 100f;

	private Player? _hiddenPlayer;

	private void TogglePlayerActiveStatusIfNeeded()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
		{
			RestorePlayer();
			return;
		}

		if (_hiddenPlayer != null && !ReferenceEquals(_hiddenPlayer, player))
			RestorePlayer();

		var playerGameObject = player.gameObject;
		if (Enabled)
		{
			if (playerGameObject.activeSelf)
				playerGameObject.SetActive(false);
			_hiddenPlayer = player;
		}
		else
		{
			RestorePlayer();
		}
	}

	protected override void Update()
	{
		base.Update();

		TogglePlayerActiveStatusIfNeeded();
	}

	protected override void UpdateWhenEnabled()
	{
		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		if (FeatureFactory.GetFeature<RavenUI>()?.Enabled == true || UI.Raven.RavenWidgets.IsCapturingKey)
			return;

		var fastMode = Input.GetKey(FastMode);
		var movementSpeed = fastMode ? FastMovementSpeed : MovementSpeed;

		var heading = Vector3.zero;
		var cameraTransform = camera.transform;

		if (Input.GetKey(Left)) heading -= cameraTransform.right;
		if (Input.GetKey(Right)) heading += cameraTransform.right;
		if (Input.GetKey(Forward)) heading += cameraTransform.forward;
		if (Input.GetKey(Backward)) heading -= cameraTransform.forward;

		if (heading != Vector3.zero)
			cameraTransform.position += movementSpeed * Time.deltaTime * heading.normalized;

		var localEulerAngles = cameraTransform.localEulerAngles;
		var newRotationX = localEulerAngles.y + Input.GetAxis(MouseXAxis) * FreeLookSensitivity;
		var newRotationY = localEulerAngles.x - Input.GetAxis(MouseYAxis) * FreeLookSensitivity;
		cameraTransform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);

		if (Input.GetKey(Teleport))
		{
			var player = GameState.Current?.LocalPlayer;
			if (!player.IsValid())
				return;

			var position = TargetFrom(cameraTransform.position);

			player.Teleport(position, false);
			Enabled = false;
		}
	}

	protected override void UpdateWhenDisabled()
	{
		RestorePlayer();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		RestorePlayer();
	}

	private void RestorePlayer()
	{
		if (_hiddenPlayer != null && !_hiddenPlayer.gameObject.activeSelf)
			_hiddenPlayer.gameObject.SetActive(true);

		_hiddenPlayer = null;
	}

	private static Vector3 TargetFrom(Vector3 cameraPosition)
	{
		return new Vector3(cameraPosition.x, cameraPosition.y - 2f, cameraPosition.z);
	}

	internal static bool TryGetTargetPosition(out Vector3 position)
	{
		position = Vector3.zero;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return false;

		position = TargetFrom(camera.transform.position);
		return true;
	}

	internal static void TeleportToCamera()
	{
		var feature = FeatureFactory.GetFeature<FreeCamera>();
		var player = GameState.Current?.LocalPlayer;

		if (feature == null || !player.IsValid() || !TryGetTargetPosition(out var position))
			return;

		player.Teleport(position, false);
		feature.Enabled = false;
		feature.RestorePlayer();
	}
}
