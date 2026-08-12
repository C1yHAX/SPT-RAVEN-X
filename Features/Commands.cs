using System;
using System.Collections.Generic;
using System.Linq;
using RavenX.ConsoleCommands;
using RavenX.Properties;
using EFT.UI;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Commands : Feature
{
	public override string Name => Strings.FeatureCommandsName;
	public override string Description => Strings.FeatureCommandsDescription;

	private bool Registered { get; set; }

	[UsedImplicitly]
	private void Update()
	{
		if (Registered || !PreloaderUI.Instantiated)
			return;

#if DEBUG
		ConsoleScreen.IAmDevShowMeLogs = true;
#endif

		RegisterCommands();
	}

	private void RegisterCommands()
	{
		foreach (var feature in Context.ToggleableFeatures.Value)
		{
			if (feature is GameState)
				continue;

			new ToggleFeatureCommand(feature)
				.Register();
		}

		foreach (var command in GetCommands())
			command.Register();

		new BuiltInCommand(Strings.CommandLoadName, () => LoadSettings())
			.Register();

		new BuiltInCommand(Strings.CommandSaveName, SaveSettings)
			.Register();

		LoadSettings(false);

		Registered = true;
	}

	private static void SaveSettings()
	{
		Configuration.ConfigurationManager.Save(Context.ConfigFile, Context.Features.Value);
	}

	private static void LoadSettings(bool warnIfNotExists = true)
	{
		Configuration.ConfigurationManager.Load(Context.ConfigFile, Context.Features.Value, warnIfNotExists);
	}

	private IEnumerable<ConsoleCommand> GetCommands()
	{
		var types = GetType()
			.Assembly
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(ConsoleCommand)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

		foreach (var type in types)
			yield return (ConsoleCommand)Activator.CreateInstance(type);
	}
}
