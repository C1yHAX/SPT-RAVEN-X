using System.Diagnostics.CodeAnalysis;
using EFT.Interactive;
using EFT;

#nullable enable

namespace RavenX.Extensions;

public static class LootItemExtensions
{
	public static bool IsValid([NotNullWhen(true)] this LootItem? lootItem)
	{
		return lootItem != null
			   && lootItem.Item.IsValid();
	}
}
