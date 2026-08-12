using System.Diagnostics.CodeAnalysis;
using EFT.Interactive;
using EFT;

#nullable enable

namespace RavenX.Extensions;

public static class ExfiltrationPointExtensions
{
	public static bool IsValid([NotNullWhen(true)] this ExfiltrationPoint? point)
	{
		return point != null
			   && point.Settings?.Name != null
			   && point.transform != null;
	}
}
