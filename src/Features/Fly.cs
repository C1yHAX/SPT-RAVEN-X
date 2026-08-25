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
	private Player? _activePlayer;
	private static readonly RaycastHit[] _groundHits = new RaycastHit[32];

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;
		var camera = state?.Camera;

		if (state == null || !player.IsValid() || camera == null)
		{
			_wasFlying = false;
			_activePlayer = null;
			return;
		}

		if (!ReferenceEquals(_activePlayer, player))
		{
			_wasFlying = false;
			_activePlayer = player;
		}

		if (!_wasFlying)
		{
			_wasFlying = true;
			_hover = player.Transform.position;
		}

		if (player.IsInventoryOpened || UI.Raven.RavenWidgets.IsCapturingKey || FeatureFactory.GetFeature<RavenUI>()?.Enabled == true)
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
		var player = _activePlayer;
		_activePlayer = null;

		if (LandOnExit && player.IsValid())
			Land(player);
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		UpdateWhenDisabled();
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

	private static void Land(Player player)
	{
		var origin = player.Transform.position + Vector3.up * 0.5f;
		var count = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
		var nearestDistance = float.MaxValue;
		var landingPoint = Vector3.zero;

		for (var i = 0; i < count; i++)
		{
			var hit = _groundHits[i];
			var hitTransform = hit.collider?.transform;
			if (hitTransform == null || hitTransform.root == player.Transform.Original.root || hit.distance >= nearestDistance)
				continue;

			nearestDistance = hit.distance;
			landingPoint = hit.point;
		}

		if (nearestDistance == float.MaxValue)
			return;

		player.Teleport(landingPoint + Vector3.up * 0.1f, false);
	}
}
