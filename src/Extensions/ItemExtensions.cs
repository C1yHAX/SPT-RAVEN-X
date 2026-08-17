using System.Diagnostics.CodeAnalysis;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using EFT;

#nullable enable

namespace RavenX.Extensions;

public static class ItemExtensions
{
	public static bool IsValid([NotNullWhen(true)] this Item? item)
	{
		return item?.Template != null;
	}

	public static bool IsFiltered(this Item item)
	{
		if (string.IsNullOrEmpty(item.TemplateId))
			return true;

		if (ItemViewFactory.IsSecureContainer(item))
			return true;

		if (item.CurrentAddress?.Container?.ParentItem?.TemplateId.ToString() == KnownTemplateIds.BossContainer)
			return true;

		return item.TemplateId.ToString() switch
		{
			KnownTemplateIds.DefaultInventory or KnownTemplateIds.Pockets => true,
			_ => false

		};
	}
}
