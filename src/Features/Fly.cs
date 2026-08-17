using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Fly : ToggleFeature
{
	public override string Name => "fly";
	public override string Description => "Move freely through the air.";

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public float Speed { get; set; } = 6f;

	[ConfigurationProperty(Order = 11)]
	public float FastSpeed { get; set; } = 18f;

	[ConfigurationProperty(Order = 20)]
	public KeyCode FastKey { get; set; } = KeyCode.LeftShift;

	[ConfigurationProperty(Order = 21)]
	public KeyCode UpKey { get; set; } = KeyCode.Space;

	[ConfigurationProperty(Order = 22)]
	public KeyCode DownKey { get; set; } = KeyCode.LeftControl;

	[ConfigurationProperty(Order = 30)]
	public bool LandOnExit { get; set; } = true;

	private bool _wasFlying;
	private Vector3 _hover;

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;
		var camera = state?.Camera;

		if (state == null || !player.IsValid() || camera == null)
			return;

		if (!_wasFlying)
		{
			_wasFlying = true;
			_hover = player.Transform.position;
		}

		if (player.IsInventoryOpened || UI.Raven.RavenWidgets.IsCapturingKey)
		{
			player.Teleport(_hover, false);
			return;
		}

		var direction = ReadDirection(camera.transform);

		if (direction != Vector3.zero)
		{
			var moveSpeed = Input.GetKey(FastKey) ? FastSpeed : Speed;
			_hover += direction * moveSpeed * Time.deltaTime;
		}

		player.Teleport(_hover, false);
	}

	protected override void UpdateWhenDisabled()
	{
		if (!_wasFlying)
			return;

		_wasFlying = false;

		if (LandOnExit)
			Land();
	}

	private Vector3 ReadDirection(Transform view)
	{

		var forward = view.forward;
		var right = view.right;
		var direction = Vector3.zero;

		if (Input.GetKey(KeyCode.W)) direction += forward;
		if (Input.GetKey(KeyCode.S)) direction -= forward;
		if (Input.GetKey(KeyCode.D)) direction += right;
		if (Input.GetKey(KeyCode.A)) direction -= right;
		if (Input.GetKey(UpKey)) direction += Vector3.up;
		if (Input.GetKey(DownKey)) direction += Vector3.down;

		return direction == Vector3.zero ? Vector3.zero : direction.normalized;
	}

	private static void Land()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var origin = player.Transform.position + Vector3.up * 0.5f;

		if (!Physics.Raycast(origin, Vector3.down, out var hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
			return;

		player.Teleport(hit.point + Vector3.up * 0.1f, false);
	}
}
