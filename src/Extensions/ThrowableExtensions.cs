using System.Diagnostics.CodeAnalysis;
using EFT;

#nullable enable

namespace RavenX.Extensions;

public static class ThrowableExtensions
{
	public static bool IsValid([NotNullWhen(true)] this Throwable? throwable)
	{
		return throwable != null
			   && throwable.transform != null;
	}
}
