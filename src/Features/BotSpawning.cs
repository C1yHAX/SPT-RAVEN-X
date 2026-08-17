using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.Game.Spawning;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AI;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class BotSpawning : ToggleFeature
{
	public override string Name => "botspawn";
	public override string Description => "Spawn bots at a chosen distance from you.";

	public override bool Enabled { get; set; } = true;

	[ConfigurationProperty(Order = 10)]
	public float Distance { get; set; } = 25f;

	[ConfigurationProperty(Order = 11)]
	public bool RandomDirection { get; set; } = false;

	[ConfigurationProperty(Order = 12)]
	public BotDifficulty Difficulty { get; set; } = BotDifficulty.normal;

	private static readonly float[] _fallbackFactors = [1f, 0.8f, 0.6f, 0.4f, 0.2f];

	internal string Status { get; private set; } = string.Empty;
	internal float LastDistance { get; private set; }

	internal bool Request(string botType)
	{
		LastDistance = 0f;

		if (!Enum.TryParse<WildSpawnType>(botType, out var spawnType))
		{
			Status = $"Failed — unknown role {botType}";
			return false;
		}

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
		{
			Status = "Failed — not in a raid";
			return false;
		}

		var controller = Singleton<IBotGame>.Instance?.BotsController;
		if (controller == null)
		{
			Status = "Failed — bots controller missing";
			return false;
		}

		if (!controller.IsEnable)
		{
			Status = "Failed — bot spawning disabled for this raid";
			return false;
		}

		var spawner = controller.BotSpawner;
		if (spawner == null)
		{
			Status = "Failed — bot spawner missing";
			return false;
		}

		if (!Enabled)
		{
			Status = $"{spawnType} requested — map picks the spot";
			ConsoleCommands.SpawnBot.SpawnBots([botType]);
			return true;
		}

		var origin = player!.Transform.position;

		if (!TryResolvePosition(player, origin, out var position))
		{
			Status = "Failed — no walkable ground that far out";
			return false;
		}

		LastDistance = Vector3.Distance(origin, position);
		Status = "Spawning…";

		SpawnAt(spawner, spawnType, position, origin);
		return true;
	}

	private bool TryResolvePosition(Player player, Vector3 origin, out Vector3 position)
	{
		position = origin;

		var heading = RandomDirection
			? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward
			: player.Transform.forward;

		heading.y = 0f;

		if (heading.sqrMagnitude < 0.0001f)
			heading = Vector3.forward;

		heading = heading.normalized;

		foreach (var factor in _fallbackFactors)
		{
			var candidate = origin + heading * (Distance * factor);

			if (Physics.Raycast(candidate + Vector3.up * 150f, Vector3.down, out var hit, 400f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
				candidate = hit.point;

			if (!NavMesh.SamplePosition(candidate, out var navHit, 12f, NavMesh.AllAreas))
				continue;

			position = navHit.position;
			return true;
		}

		return false;
	}

	private async void SpawnAt(BotSpawner spawner, WildSpawnType spawnType, Vector3 position, Vector3 lookAt)
	{
		try
		{
			var zone = spawner.GetClosestZone(position, out _);
			if (zone == null)
			{
				Status = "Failed — no bot zone near that spot";
				return;
			}

			var profileParams = new GetProfileDataParams(EPlayerSide.Savage, spawnType, Difficulty, 5f, null, false);
			var data = await BotCreationData.Create(profileParams, spawner._botCreator, 1, spawner);

			if (data == null)
			{
				Status = "Failed — profile could not be built";
				return;
			}

			var templates = spawner._spawnSystem?.SelectAISpawnPoints(data, zone, 1, null, ActionIfNotEnoughPoints.FillWithDiscardedPointsAndDuplicates, null);
			if (templates == null || templates.Count == 0)
			{
				Status = "Failed — map has no usable spawn point";
				return;
			}

			var facing = Vector3.ProjectOnPlane(lookAt - position, Vector3.up);
			var rotation = facing.sqrMagnitude < 0.0001f ? Quaternion.identity : Quaternion.LookRotation(facing.normalized);

			var points = new List<ISpawnPoint> { new PlacedSpawnPoint(templates[0], position, rotation) };

			spawner.TryToSpawnInZoneInner(zone, data, 1, false, true, points, true);

			Status = $"{spawnType} spawned at {LastDistance:0}m";
		}
		catch (Exception e)
		{
			Status = $"Failed — {e.Message}";
		}
	}
}

internal sealed class PlacedSpawnPoint(ISpawnPoint inner, Vector3 position, Quaternion rotation) : ISpawnPoint
{
	public Vector3 Position { get; } = position;
	public Quaternion Rotation { get; } = rotation;

	public string Id => inner.Id;
	public string Name => inner.Name;
	public EPlayerSideMask Sides => inner.Sides;
	public ESpawnCategoryMask Categories => inner.Categories;
	public string Infiltration => inner.Infiltration;
	public string BotZoneName => inner.BotZoneName;
	public bool IsSnipeZone => inner.IsSnipeZone;
	public float DelayToCanSpawnSec => 0f;
	public ISpawnPointCollider Collider => inner.Collider;

	public bool SpawnBlocked
	{
		get => false;
		set => inner.SpawnBlocked = value;
	}

	public float NextBornTime
	{
		get => 0f;
		set => inner.NextBornTime = value;
	}

	public int CorePointId
	{
		get => inner.CorePointId;
		set => inner.CorePointId = value;
	}

	public float CalcMultiSpawnDelay(float additionalDelay, BotCreationData creationData) => 0f;
	public bool IsNotCollidedArtillery(ArtilleryShellingControllerServer artilleryShelling) => true;
	public bool IsInPlayersIndividualLimits(BotCreationData creationData) => true;
	public void IncreaseUsedPlayerSpawnsForNearestPlayer(BotCreationData creationData) { }
	public void Dispose() { }
}
