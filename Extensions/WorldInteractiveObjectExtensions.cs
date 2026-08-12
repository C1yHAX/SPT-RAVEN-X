using System.Diagnostics.CodeAnalysis;
using EFT.Interactive;
using EFT;

#nullable enable

namespace RavenX.Extensions;

public static class WorldInteractiveObjectExtensions
{
	public static bool IsValid([NotNullWhen(true)] this WorldInteractiveObject? obj)
	{
		return obj != null;
	}
}
