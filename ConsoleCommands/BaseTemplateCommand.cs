using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

internal abstract class BaseTemplateCommand : ConsoleCommandWithArgument
{
	public override string Pattern => RequiredArgumentPattern;
}
