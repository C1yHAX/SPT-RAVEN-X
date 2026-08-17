using System;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

internal class BuiltInCommand(string name, Action action) : ConsoleCommandWithoutArgument
{
	public override string Name => name;

	public override void Execute()
	{
		action();
	}
}
