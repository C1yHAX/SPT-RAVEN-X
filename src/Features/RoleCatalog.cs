using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal readonly struct RoleDefinition(string key, string label, string group)
{
	public string Key { get; } = key;
	public string Label { get; } = label;
	public string Group { get; } = group;
}

internal static class RoleCatalog
{
	public const string PmcBearKey = "PMC-BEAR";
	public const string PmcUsecKey = "PMC-USEC";

	private static readonly Dictionary<string, string> _roleLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		["marksman"] = "Sniper Scav",
		["assault"] = "Scav",
		["assaultGroup"] = "Scav Group",
		["cursedAssault"] = "Cursed Scav",
		["bossBully"] = "Reshala",
		["followerBully"] = "Reshala Guard",
		["bossKilla"] = "Killa",
		["bossKojaniy"] = "Shturman",
		["followerKojaniy"] = "Shturman Guard",
		["pmcBot"] = "Raider",
		["bossGluhar"] = "Glukhar",
		["followerGluharAssault"] = "Glukhar Assault",
		["followerGluharSecurity"] = "Glukhar Security",
		["followerGluharScout"] = "Glukhar Scout",
		["followerGluharSnipe"] = "Glukhar Sniper",
		["bossSanitar"] = "Sanitar",
		["followerSanitar"] = "Sanitar Guard",
		["sectantWarrior"] = "Cultist Warrior",
		["sectantPriest"] = "Cultist Priest",
		["bossTagilla"] = "Tagilla",
		["followerTagilla"] = "Tagilla Guard",
		["exUsec"] = "Rogue",
		["bossKnight"] = "Knight",
		["followerBigPipe"] = "Big Pipe",
		["followerBirdEye"] = "Birdeye",
		["bossZryachiy"] = "Zryachiy",
		["followerZryachiy"] = "Zryachiy Guard",
		["bossBoar"] = "Kaban",
		["followerBoar"] = "Kaban Guard",
		["bossBoarSniper"] = "Kaban Sniper",
		["followerBoarClose1"] = "Kaban Guard 1",
		["followerBoarClose2"] = "Kaban Guard 2",
		["bossKolontay"] = "Kollontay",
		["followerKolontayAssault"] = "Kollontay Assault",
		["followerKolontaySecurity"] = "Kollontay Security",
		["shooterBTR"] = "BTR Gunner",
		["bossPartisan"] = "Partisan",
		["sectantPredvestnik"] = "Cultist Harbinger",
		["sectantPrizrak"] = "Cultist Ghost",
		["sectantOni"] = "Cultist Oni",
		["infectedAssault"] = "Infected Scav",
		["infectedPmc"] = "Infected PMC",
		["infectedCivil"] = "Infected Civilian",
		["infectedLaborant"] = "Infected Lab Worker",
		["infectedTagilla"] = "Infected Tagilla",
		["bossTagillaAgro"] = "Tagilla Aggressive",
		["bossKillaAgro"] = "Killa Aggressive",
		["tagillaHelperAgro"] = "Tagilla Helper Aggressive",
		["peacefullZryachiyEvent"] = "Peaceful Zryachiy Event",
		["sectactPriestEvent"] = "Cultist Priest Event",
		["ravangeZryachiyEvent"] = "Zryachiy Revenge Event"
	};

	private static readonly List<RoleDefinition> _definitions = Build();
	private static readonly string[] _groups = [.. _definitions.Select(d => d.Group).Distinct()];

	public static IReadOnlyList<RoleDefinition> Definitions => _definitions;
	public static IReadOnlyList<string> Groups => _groups;

	private static List<RoleDefinition> Build()
	{
		var definitions = new List<RoleDefinition>
		{
			new(PmcBearKey, "BEAR", "PMC"),
			new(PmcUsecKey, "USEC", "PMC")
		};
		var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			PmcBearKey,
			PmcUsecKey
		};

		foreach (WildSpawnType role in Enum.GetValues(typeof(WildSpawnType)))
		{
			if (role is WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC)
				continue;

			var name = role.ToString();
			var key = "ROLE-" + name;
			if (keys.Add(key))
				definitions.Add(new RoleDefinition(key, LabelFor(name), GroupOf(role, name)));
		}

		return definitions;
	}

	private static string GroupOf(WildSpawnType role, string name)
	{
		if (role is WildSpawnType.assault or WildSpawnType.assaultGroup or WildSpawnType.marksman or WildSpawnType.cursedAssault)
			return "Scav";

		if (name.StartsWith("boss", StringComparison.OrdinalIgnoreCase) || IsFollower(name))
			return "Boss";

		if (name.StartsWith("sect", StringComparison.OrdinalIgnoreCase))
			return "Cultist";

		if (name.StartsWith("infected", StringComparison.OrdinalIgnoreCase))
			return "Infected";

		if (role == WildSpawnType.exUsec || name.IndexOf("pmc", StringComparison.OrdinalIgnoreCase) >= 0)
			return "Raider";

		return "Special";
	}

	private static bool IsFollower(string name) => name.StartsWith("follower", StringComparison.OrdinalIgnoreCase);

	private static string LabelFor(string name)
	{
		if (_roleLabels.TryGetValue(name, out var label))
			return label;

		var result = new StringBuilder(name.Length + 8);
		for (var i = 0; i < name.Length; i++)
		{
			var current = name[i];
			if (current is '_' or '-')
			{
				if (result.Length > 0 && result[result.Length - 1] != ' ')
					result.Append(' ');
				continue;
			}

			if (i > 0 && result.Length > 0 && result[result.Length - 1] != ' '
				&& (char.IsUpper(current) || char.IsDigit(current) && !char.IsDigit(name[i - 1]) || !char.IsDigit(current) && char.IsDigit(name[i - 1])))
				result.Append(' ');

			result.Append(result.Length == 0 ? char.ToUpperInvariant(current) : current);
		}

		return result.ToString();
	}

	public static string KeyOf(Player player)
	{
		var info = player.Profile?.Info;
		var role = info?.Settings?.Role;

		if (role == WildSpawnType.pmcBEAR)
			return PmcBearKey;

		if (role == WildSpawnType.pmcUSEC)
			return PmcUsecKey;

		if (role.HasValue)
			return "ROLE-" + role.Value;

		return info?.Side == EPlayerSide.Bear ? PmcBearKey : PmcUsecKey;
	}

	public static Color DefaultColorFor(string key, string group)
	{
		if (key == PmcBearKey)
			return new Color(0.32f, 0.55f, 1f);

		if (key == PmcUsecKey)
			return new Color(0.35f, 0.85f, 0.45f);

		return group switch
		{
			"Scav" => new Color(0.95f, 0.82f, 0.30f),
			"Boss" => new Color(1f, 0.30f, 0.30f),
			"Cultist" => new Color(0.70f, 0.35f, 0.95f),
			"Infected" => new Color(0.55f, 0.75f, 0.35f),
			"Raider" => new Color(1f, 0.55f, 0.20f),
			_ => new Color(0.80f, 0.80f, 0.80f)
		};
	}

	public static Color OccludedFrom(Color visible) => new(visible.r * 0.35f, visible.g * 0.35f, visible.b * 0.35f, 0.8f);

	private static Dictionary<string, string>? _labels;

	public static string LabelOf(string key)
	{
		if (_labels == null)
		{
			_labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var definition in _definitions)
				_labels[definition.Key] = definition.Label;
		}

		return _labels.TryGetValue(key, out var label) ? label : key;
	}

	public static RoleSetting Resolve(List<RoleSetting> settings, ref Dictionary<string, RoleSetting>? index, string key)
	{
		if (index == null || index.Count != settings.Count)
			RebuildIndex(settings, ref index);

		if (index!.TryGetValue(key, out var found) && string.Equals(found.Key, key, StringComparison.OrdinalIgnoreCase))
			return found;

		foreach (var entry in settings)
		{
			if (entry != null && string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
			{
				index[key] = entry;
				return entry;
			}
		}

		var group = "Special";

		foreach (var definition in _definitions)
		{
			if (definition.Key != key)
				continue;

			group = definition.Group;
			break;
		}

		var visible = DefaultColorFor(key, group);
		var setting = new RoleSetting(key, visible, OccludedFrom(visible));

		settings.Add(setting);
		index[key] = setting;

		return setting;
	}

	private static void RebuildIndex(List<RoleSetting> settings, ref Dictionary<string, RoleSetting>? index)
	{
		index = new Dictionary<string, RoleSetting>(StringComparer.OrdinalIgnoreCase);

		for (var i = settings.Count - 1; i >= 0; i--)
		{
			var entry = settings[i];
			if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || index.ContainsKey(entry.Key))
			{
				settings.RemoveAt(i);
				continue;
			}

			index[entry.Key] = entry;
		}
	}
}
