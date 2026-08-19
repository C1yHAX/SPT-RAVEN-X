using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using RavenX.UI;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal abstract class PointOfInterests : CachableFeature<PointOfInterest>
{
	internal class ObjectPool<T>(Func<T> objectGenerator)
	{
		private readonly ConcurrentBag<T> _objects = [];

		public T Get() => _objects.TryTake(out var item) ? item : objectGenerator();

		public void Return(T item) => _objects.Add(item);
	}

	internal class PointOfInterestPool() : ObjectPool<PointOfInterest>(() => new PointOfInterest());

	public static PointOfInterestPool Pool = new();

	[ConfigurationProperty]
	public float MaximumDistance { get; set; } = 0f;

	[ConfigurationProperty]
	public bool CullInScopes { get; set; } = false;

	public abstract Color GroupingColor { get; }

	protected override void BeforeRefreshData(IReadOnlyList<PointOfInterest> data)
	{

		foreach (var poi in data)
			Pool.Return(poi);
	}

	private static Vector3 LivePosition(PointOfInterest poi)
	{
		var follow = poi.Follow;

		if ((object?)follow == null)
			return poi.Position;

		return follow != null ? follow.position : poi.Position;
	}

	public override void ProcessDataOnGUI(IReadOnlyList<PointOfInterest> data)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var snapshot = GameState.Current;
		if (snapshot == null)
			return;

		if (CullInScopes && !snapshot.MapMode && Players.IsScoped())
			return;

		var camera = snapshot.MapMode ? snapshot.MapCamera : snapshot.Camera;
		if (camera == null)
			return;

		var cameraPosition = camera.transform.position;
		var poiPerPosition = data.ToLookup(LivePosition);
		foreach (var positionGroup in poiPerPosition)
		{
			var position = positionGroup.Key;
			var screenPosition = camera.WorldPointToVisibleScreenPoint(position);
			if (screenPosition == Vector2.zero)
				continue;

			if (snapshot.MapMode)
				cameraPosition.y = position.y;

			var distance = Mathf.Round(Vector3.Distance(cameraPosition, position));
			if (MaximumDistance > 0 && distance > MaximumDistance)
				continue;

			var drawPosition = screenPosition;

			var poiPerOwner = positionGroup.ToLookup(poi => poi.Owner);
			foreach (var ownerGroup in poiPerOwner)
			{
				var distinctGroup = ownerGroup
					.DistinctBy(poi => poi.Name)
					.ToList();

				var owner = ownerGroup.Key;
				var flags = GetCaptionFlags.All;

				if (owner != null && distinctGroup.Count > 1)
				{
					flags = GetCaptionFlags.Name;
					var distanceText = string.Format(Strings.FeaturePointOfInterestsDistanceFormat, distance);
					drawPosition = new Vector2(drawPosition.x, drawPosition.y + Render.DrawString(drawPosition, string.Format(Strings.FeaturePointOfInterestsGroupFormat, owner, distanceText), GroupingColor, false).y);
				}

				foreach (var poi in distinctGroup)
				{
					drawPosition = new Vector2(drawPosition.x, drawPosition.y + Render.DrawString(drawPosition, GetCaption(poi, distance, flags), poi.Color, flags == GetCaptionFlags.All).y);
				}
			}
		}
	}

	[Flags]
	public enum GetCaptionFlags
	{
		Name = 1,
		Owner = 2,
		Distance = 4,
		All = Name | Owner | Distance
	}

	public virtual string GetCaption(PointOfInterest poi, double distance, GetCaptionFlags flags = GetCaptionFlags.All)
	{
		var nameText = string.Empty;
		var distanceText = string.Empty;
		var ownerText = string.Empty;

		if ((flags & GetCaptionFlags.Name) != 0)
			nameText = poi.Name;

		if (poi.Owner != null && (flags & GetCaptionFlags.Owner) != 0)
			ownerText = string.Format(Strings.FeaturePointOfInterestsOwnerFormat, poi.Owner);

		if ((flags & GetCaptionFlags.Distance) != 0)
			distanceText = string.Format(Strings.FeaturePointOfInterestsDistanceFormat, distance);

		return string
			.Format(Strings.FeaturePointOfInterestsFormat, nameText, ownerText, distanceText)
			.Replace("  ", " ")
			.Trim();
	}
}
