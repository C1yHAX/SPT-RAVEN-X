using System.Text.RegularExpressions;
using RavenX.Configuration;
using JsonType;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

internal abstract class BaseTrackCommand : ConsoleCommandWithArgument
{
	private static string ColorNames => string.Join("|", ColorConverter.ColorNames());
	public override string Pattern => $"(?<{ValueGroup}>.+?)(?<{ExtraGroup}> ({ColorNames}|\\[[\\.,\\d ]*\\]{{1}}))?";
	protected virtual ELootRarity? Rarity => null;

	public override void Execute(Match match)
	{
		TrackLootItem(match, Rarity);
	}

	private void TrackLootItem(Match match, ELootRarity? rarity = null)
	{
		var matchGroup = match.Groups[ValueGroup];
		if (matchGroup is not { Success: true })
			return;

		Color? color = null;
		var extraGroup = match.Groups[ExtraGroup];
		if (extraGroup is { Success: true })
			color = ColorConverter.Parse(extraGroup.Value);

		TrackList.ShowTrackList(this, LootItemsFeature, LootItemsFeature.Track(matchGroup.Value, color, rarity));
	}
}
