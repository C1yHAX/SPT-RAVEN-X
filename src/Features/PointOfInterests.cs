using System;
using System.Collections.Generic;
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
	internal sealed class PointOfInterestPool
	{
		public PointOfInterest Get() => default;
		public void Return(PointOfInterest item) { }
	}

	private struct GroupedPoint
	{
		public PointOfInterest Point;
		public int Count;
	}

	private sealed class OwnerGroup
	{
		public string? Owner;
		public readonly List<GroupedPoint> Points = [];
		public readonly Dictionary<string, int> NameIndexes = new(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class PositionGroup
	{
		public Vector3 Position;
		public readonly List<OwnerGroup> Owners = [];
	}

	public static readonly PointOfInterestPool Pool = new();

	private readonly Dictionary<Vector3, PositionGroup> _positions = [];
	private readonly List<PositionGroup> _positionGroups = [];
	private readonly Stack<PositionGroup> _freePositions = [];
	private readonly Stack<OwnerGroup> _freeOwners = [];

	[ConfigurationProperty]
	public float MaximumDistance { get; set; } = 0f;

	[ConfigurationProperty]
	public bool CullInScopes { get; set; } = false;

	public abstract Color GroupingColor { get; }

	protected override void BeforeRefreshData(IReadOnlyList<PointOfInterest> data)
	{
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

		BuildGroups(data);

		var cameraPosition = camera.transform.position;
		foreach (var positionGroup in _positionGroups)
		{
			var position = positionGroup.Position;
			var screenPosition = camera.WorldPointToVisibleScreenPoint(position);
			if (screenPosition == Vector2.zero)
				continue;

			var distanceOrigin = cameraPosition;
			if (snapshot.MapMode)
				distanceOrigin.y = position.y;

			var offset = position - distanceOrigin;
			if (MaximumDistance > 0f && offset.sqrMagnitude > MaximumDistance * MaximumDistance)
				continue;

			var distance = Mathf.Round(offset.magnitude);

			var drawPosition = screenPosition;

			foreach (var ownerGroup in positionGroup.Owners)
			{
				var owner = ownerGroup.Owner;
				var flags = GetCaptionFlags.All;

				if (owner != null && ownerGroup.Points.Count > 1)
				{
					flags = GetCaptionFlags.Name;
					var distanceText = string.Format(Strings.FeaturePointOfInterestsDistanceFormat, distance);
					drawPosition = new Vector2(drawPosition.x, drawPosition.y + Render.DrawString(drawPosition, string.Format(Strings.FeaturePointOfInterestsGroupFormat, owner, distanceText), GroupingColor, false).y);
				}

				foreach (var grouped in ownerGroup.Points)
				{
					var poi = grouped.Point;
					if (grouped.Count > 1)
						poi.Name = $"{poi.Name} ×{grouped.Count}";

					drawPosition = new Vector2(drawPosition.x, drawPosition.y + Render.DrawString(drawPosition, GetCaption(poi, distance, flags), poi.Color, flags == GetCaptionFlags.All).y);
				}
			}
		}
	}

	private void BuildGroups(IReadOnlyList<PointOfInterest> data)
	{
		RecycleGroups();

		for (var i = 0; i < data.Count; i++)
		{
			var poi = data[i];
			var position = LivePosition(poi);

			if (!_positions.TryGetValue(position, out var positionGroup))
			{
				positionGroup = _freePositions.Count > 0 ? _freePositions.Pop() : new PositionGroup();
				positionGroup.Position = position;
				_positions[position] = positionGroup;
				_positionGroups.Add(positionGroup);
			}

			OwnerGroup? ownerGroup = null;
			for (var ownerIndex = 0; ownerIndex < positionGroup.Owners.Count; ownerIndex++)
			{
				var candidate = positionGroup.Owners[ownerIndex];
				if (string.Equals(candidate.Owner, poi.Owner, StringComparison.OrdinalIgnoreCase))
				{
					ownerGroup = candidate;
					break;
				}
			}

			if (ownerGroup == null)
			{
				ownerGroup = _freeOwners.Count > 0 ? _freeOwners.Pop() : new OwnerGroup();
				ownerGroup.Owner = poi.Owner;
				positionGroup.Owners.Add(ownerGroup);
			}

			if (ownerGroup.NameIndexes.TryGetValue(poi.Name, out var pointIndex))
			{
				var grouped = ownerGroup.Points[pointIndex];
				grouped.Count++;
				ownerGroup.Points[pointIndex] = grouped;
				continue;
			}

			ownerGroup.NameIndexes[poi.Name] = ownerGroup.Points.Count;
			ownerGroup.Points.Add(new GroupedPoint { Point = poi, Count = 1 });
		}
	}

	private void RecycleGroups()
	{
		for (var i = 0; i < _positionGroups.Count; i++)
		{
			var position = _positionGroups[i];

			for (var ownerIndex = 0; ownerIndex < position.Owners.Count; ownerIndex++)
			{
				var owner = position.Owners[ownerIndex];
				owner.Owner = null;
				owner.Points.Clear();
				owner.NameIndexes.Clear();
				_freeOwners.Push(owner);
			}

			position.Owners.Clear();
			_freePositions.Push(position);
		}

		_positionGroups.Clear();
		_positions.Clear();
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
