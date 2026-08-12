using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class TemplateHelper
{

	private static readonly Dictionary<string, ItemTemplate> _templates = [];

	private static void UpdateTemplates()
	{
#if !EFT_LIVE
		if (!Singleton<ItemFactory>.Instantiated)
			return;

		var mongoTemplates = Singleton<ItemFactory>
			.Instance
			.ItemTemplates;

		if (_templates.Count == mongoTemplates.Count)
			return;

		foreach (var kv in mongoTemplates)
		{
			_templates.Add(kv.Key.ToString(), kv.Value);
		}
#endif
	}

	internal static ItemTemplate[] AllTemplates()
	{
		UpdateTemplates();
		return [.. _templates.Values];
	}

	internal static string GetSubcategory(ItemTemplate template)
	{
		UpdateTemplates();

		if (template.ParentId is not { } parentId)
			return string.Empty;

		if (!_templates.TryGetValue(parentId.ToString(), out var parent))
			return string.Empty;

		return parent.NameLocalizationKey.Localized();
	}

	internal static string GetCategory(ItemTemplate template)
	{
		UpdateTemplates();

		var current = template;

		for (var depth = 0; depth < 12; depth++)
		{
			if (current.ParentId is not { } parentId)
				break;

			if (!_templates.TryGetValue(parentId.ToString(), out var parent))
				break;

			if (parent.ParentId == null)
				break;

			current = parent;
		}

		var name = current.NameLocalizationKey.Localized();
		return name.Length > 0 ? name : "Other";
	}

	internal static ItemTemplate[] FindTemplates(string searchShortNameOrTemplateId)
	{
		UpdateTemplates();

		if (_templates.TryGetValue(searchShortNameOrTemplateId, out var template))
		{
			return [template];
		}

		return [.. _templates
			.Values
			.Where(t => t.ShortNameLocalizationKey.Localized().IndexOf(searchShortNameOrTemplateId, StringComparison.OrdinalIgnoreCase) >= 0
						|| t.NameLocalizationKey.Localized().IndexOf(searchShortNameOrTemplateId, StringComparison.OrdinalIgnoreCase) >= 0)];
	}
}
