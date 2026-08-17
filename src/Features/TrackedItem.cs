using RavenX.Configuration;
using JsonType;
using Newtonsoft.Json;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class TrackedItem(string name, Color? color = null, ELootRarity? rarity = null)
{
	public const string MatchAll = "*";
	public string Name { get; set; } = name;

	[JsonConverter(typeof(ColorConverter))]
	public Color? Color { get; set; } = color;

	public ELootRarity? Rarity { get; set; } = rarity;

	[JsonIgnore]
	public bool IsMatchAll => Name == MatchAll;
}
