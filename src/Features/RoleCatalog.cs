using System;
using System.Collections.Generic;
using System.Linq;
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

		foreach (WildSpawnType role in Enum.GetValues(typeof(WildSpawnType)))
		{
			if (role is WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC)
				continue;

			var name = role.ToString();
			definitions.Add(new RoleDefinition("ROLE-" + name, name, GroupOf(role, name)));
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
		{
			index = new Dictionary<string, RoleSetting>(StringComparer.OrdinalIgnoreCase);

			foreach (var entry in settings)
				index[entry.Key] = entry;
		}

		if (index.TryGetValue(key, out var found))
			return found;

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
}
