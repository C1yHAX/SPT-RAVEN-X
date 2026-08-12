using System.Text.RegularExpressions;
using RavenX.Features;
using RavenX.Properties;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

internal class ToggleFeatureCommand(ToggleFeature feature) : ConsoleCommandWithArgument
{
	public override string Name => feature.Name;
	public override string Pattern => $"(?<{ValueGroup}>({Strings.TextOn})|({Strings.TextOff}))";

	public override void Execute(Match match)
	{
		var matchGroup = match.Groups[ValueGroup];
		if (matchGroup is not { Success: true })
			return;

		var value = matchGroup.Value;
		feature.Enabled = value == Strings.TextOn;
	}
}
