using RavenX.Features;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class Status : ConsoleCommandWithoutArgument
{
	public override string Name => Strings.CommandStatus;

	private static string GetFeatureHelpText(ToggleFeature feature)
	{
		var toggleKey = feature.Key != KeyCode.None ? string.Format(Strings.CommandStatusTextToggleFormat, feature.Key) : string.Empty;
		return string
			.Format(Strings.CommandStatusTextFormat, feature.Name, feature.Enabled ? Strings.TextOn.Green() : Strings.TextOff.Red(), toggleKey)
			.Trim();
	}

	public override void Execute()
	{
		foreach (var feature in Context.ToggleableFeatures.Value)
		{
			if (feature is GameState)
				continue;

			AddConsoleLog(GetFeatureHelpText(feature));
		}
	}
}
