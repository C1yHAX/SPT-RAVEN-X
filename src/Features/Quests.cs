using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.Counters;
using EFT.Interactive;
using EFT.Quests;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Quests : PointOfInterests
{
	public override string Name => Strings.FeatureQuestsName;
	public override string Description => Strings.FeatureQuestsDescription;

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.magenta;

	public override float CacheTimeInSec { get; set; } = 5f;
	public override bool Enabled { get; set; } = false;
	public override Color GroupingColor => Color;

	private readonly ConcurrentDictionary<string, ExperienceTrigger[]> _experienceTriggerCache = [];
	private readonly ConcurrentDictionary<string, PlaceItemTrigger[]> _placeItemTriggerCache = [];
	private static bool _refreshLookupTables = true;
	private int _sceneHandle = -1;

	[UsedImplicitly]
	protected static void OnConditionChangedHandlerPostfix()
	{
		_refreshLookupTables = true;
	}

	public override void RefreshData(List<PointOfInterest> data)
	{
		var world = Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var profile = player.Profile;
		if (profile == null)
			return;

		var scene = SceneManager.GetActiveScene();
		if (!scene.isLoaded)
			return;

		var startedQuests = profile.QuestsData
			.Where(q => q.Status is EQuestStatus.Started && q.Template != null)
			.ToArray();

		if (startedQuests.Length == 0)
			return;

		if (_refreshLookupTables || _sceneHandle != scene.handle)
		{
			_experienceTriggerCache.Clear();
			_placeItemTriggerCache.Clear();

			_refreshLookupTables = false;
			_sceneHandle = scene.handle;
		}

		RefreshPlaceOrRepairItemLocations(scene, startedQuests, profile, data);
		RefreshVisitPlaceLocations(scene, startedQuests, profile, data);
		RefreshFindItemLocations(startedQuests, world, data);
	}

	private void RefreshVisitPlaceLocations(Scene scene, QuestDataClass[] startedQuests, Profile profile, List<PointOfInterest> records)
	{
		if (!_experienceTriggerCache.TryGetValue(scene.name, out var triggers))
		{
			triggers = FindObjectsOfType<ExperienceTrigger>();
			if (triggers.Length > 0)
				_experienceTriggerCache[scene.name] = triggers;
		}

		foreach (var quest in startedQuests)
		{
			if (!quest.Template!.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var finishConditions))
				continue;

			var conditions = finishConditions.OfType<ConditionCounterCreator>().ToArray();
			foreach (var condition in conditions)
			{
				if (quest.CompletedConditions.Contains(condition.id))
					continue;

				foreach (var cvp in condition.Conditions.OfType<ConditionVisitPlace>())
				{
					var trigger = triggers.FirstOrDefault(t => t.Id == cvp.target);
					if (trigger == null)
						continue;

					var visited = profile.Stats.Eft.OverallCounters.GetInt(CounterTag.TriggerVisited, trigger.Id) > 0;
					if (visited)
						continue;

					var position = trigger.transform.position;
					AddQuestRecord(records, condition, quest, position);
					break;
				}
			}
		}
	}

	private void RefreshFindItemLocations(QuestDataClass[] startedQuests, GameWorld world, List<PointOfInterest> records)
	{
		var lootItems = world.LootItems;

		for (var i = 0; i < lootItems.Count; i++)
		{
			var lootItem = lootItems.GetByIndex(i);
			if (!lootItem.IsValid())
				continue;

			if (!lootItem.Item.QuestItem)
				continue;

			foreach (var quest in startedQuests)
			{
				if (!quest.Template!.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var finishConditions))
					continue;

				foreach (var condition in finishConditions.OfType<ConditionFindItem>())
				{
					if (!condition.target.Contains(lootItem.Item.TemplateId.ToString()) || quest.CompletedConditions.Contains(condition.id))
						continue;

					var position = lootItem.transform.position;
					AddQuestRecord(records, condition, quest, position);
				}
			}
		}
	}

	private void RefreshPlaceOrRepairItemLocations(Scene scene, QuestDataClass[] startedQuests, Profile profile, List<PointOfInterest> records)
	{
		var allPlayerItems = profile
			.Inventory
			.GetPlayerItems()
			.ToArray();

		if (!_placeItemTriggerCache.TryGetValue(scene.name, out var triggers))
		{
			triggers = FindObjectsOfType<PlaceItemTrigger>();
			if (triggers.Length > 0)
				_placeItemTriggerCache[scene.name] = triggers;
		}

		foreach (var quest in startedQuests)
		{
			if (!quest.Template!.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var finishConditions))
				continue;

			var conditions = finishConditions.OfType<ConditionZone>().ToArray();
			foreach (var condition in conditions)
			{
				if (quest.CompletedConditions.Contains(condition.id))
					continue;

				var result = allPlayerItems.FirstOrDefault(x => condition.target.Contains(x.TemplateId.ToString()));
				if (result == null)
					continue;

				var trigger = triggers.FirstOrDefault(t => t.Id == condition.zoneId);
				if (trigger == null)
					continue;

				var position = trigger.transform.position;
				AddQuestRecord(records, condition, quest, position);
				break;
			}
		}
	}

	private void AddQuestRecord(List<PointOfInterest> records, Condition condition, QuestDataClass quest, Vector3 position)
	{
		var poi = Pool.Get();
		poi.Name = string.Format(Strings.FeatureQuestsFormat, condition.FormattedDescription, quest.Template!.Name);
		poi.Position = position;
		poi.Color = Color;
		poi.Owner = null;

		records.Add(poi);
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(QuestController), nameof(QuestController.OnConditionChangedHandler), nameof(OnConditionChangedHandlerPostfix));
		});
	}
}
