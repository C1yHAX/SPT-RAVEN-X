using RavenX.Properties;
using EFT.UI;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

internal abstract class ConsoleCommandWithoutArgument : ConsoleCommand
{
	public abstract void Execute();

	public override void Register()
	{
#if DEBUG
		AddConsoleLog(string.Format(Strings.DebugRegisteringCommandFormat, Name));
#endif
		ConsoleScreen.Processor.RegisterCommand(Name, Execute);
	}
}
