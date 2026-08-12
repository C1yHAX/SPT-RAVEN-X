using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using RavenX.UI;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Map : BaseMapToggleFeature
{
	public override string Name => Strings.FeatureMapName;
	public override string Description => Strings.FeatureMapDescription;

	[ConfigurationProperty(Skip = true)]
	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 20)]
	public float Range { get; set; } = 400f;

	protected override void OnGUIWhenEnabled()
	{
		if (Range <= 0)
			return;

		var snapshot = GameState.Current;
		if (snapshot == null)
			return;

		snapshot.MapMode = true;

		var camera = snapshot.Camera;
		if (camera == null)
			return;

		var hostiles = snapshot.Hostiles;
		var width = Screen.currentResolution.width;
		var height = Screen.currentResolution.height;

		SetupMapCamera(camera, 0, 0, width, height);
		UpdateMapCamera(camera, Range);

		if (MapCamera == null)
			return;

		if (snapshot.MapCamera != MapCamera)
			snapshot.MapCamera = MapCamera;

		Render.DrawPlayer(Render.ScreenCenter, 10f, Color.white, 2f);

		DrawHostiles(MapCamera, hostiles, Range);
	}

	protected override void UpdateWhenDisabled()
	{
		ToggleMapCameraIfNeeded(false);

		var snapshot = GameState.Current;
		if (snapshot == null)
			return;

		snapshot.MapMode = false;
	}

	protected override Vector2 GetTargetPosition(Vector3 playerPosition, Vector3 targetPosition, float playerEulerY)
	{
		if (MapCamera == null)
			return Vector2.zero;

		return MapCamera.WorldPointToScreenPoint(targetPosition);
	}

	protected override void AdjustTargetPositionForRender(ref Vector2 position)
	{

	}
}
