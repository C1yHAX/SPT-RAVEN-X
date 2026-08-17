using System.Collections.Generic;
using System.Linq;
using EFT.Quests;
using RavenX.Extensions;
using RavenX.Features;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class SpawnQuestItems : ConsoleCommandWithoutArgument
{
	public override string Name => Strings.CommandSpawnQuestItems;

	public override void Execute()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var profile = player.Profile;

		var startedQuests = profile.QuestsData
			.Where(q => q.Status is EQuestStatus.Started && q.Template != null)
			.ToArray();

		if (!startedQuests.Any())
			return;

		foreach (var quest in startedQuests)
		{
			foreach (var condition in GetConditions(quest))
			{
				var count = Mathf.RoundToInt(condition.value);
				if (count is <= 0 or > 20)
					continue;

				foreach (var target in condition.target)
				{
					for (var i = 0; i < count; i++)
						Spawn.SpawnTemplate(target, player, this, t => !t.QuestItem);
				}
			}
		}
	}

	private static IEnumerable<ConditionMultipleTargets> GetConditions(QuestDataClass quest)
	{

		var conditions = quest.Template!.Conditions[EQuestStatus.AvailableForFinish];
		return conditions.OfType<ConditionFindItem>();
	}
}
