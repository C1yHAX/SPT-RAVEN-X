using System;
using System.Reflection;
using EFT.InputSystem;
using EFT.UI;
using Newtonsoft.Json;
using EFT;

#nullable enable

namespace RavenX.Features;

internal interface IFeature
{
	[JsonIgnore]
	public string Name { get; }
}

internal abstract class Feature : InputNode, IFeature
{
	public abstract string Name { get; }
	public abstract string Description { get; }

	private string? _harmonyId = null;

	public void HarmonyPatchOnce(Action<HarmonyLib.Harmony> action)
	{
		if (_harmonyId != null)
			return;

		var harmonyId = GetType().FullName ?? GetType().Name;
		var harmony = new HarmonyLib.Harmony(harmonyId);
		try
		{
			action(harmony);
			_harmonyId = harmonyId;
		}
		catch
		{
			try
			{
				Unpatch(harmony, harmonyId);
			}
			catch
			{
			}
			throw;
		}
	}

	private static void Unpatch(HarmonyLib.Harmony harmony, string harmonyId)
	{
		var unpatchSelf = HarmonyLib.AccessTools.Method(typeof(HarmonyLib.Harmony), "UnpatchSelf", Type.EmptyTypes);
		if (unpatchSelf != null)
		{
			unpatchSelf.Invoke(harmony, null);
			return;
		}

		var unpatchAll = HarmonyLib.AccessTools.Method(typeof(HarmonyLib.Harmony), "UnpatchAll", [typeof(string)]);
		unpatchAll?.Invoke(harmony, [harmonyId]);
	}

	public void HarmonyDispatch(HarmonyLib.Harmony harmony, Type originalType, string? originalMethod, string? newPrefixMethod, string? newPostfixMethod, Type[]? parameters = null)
	{
		MethodBase original = originalMethod == null
			? HarmonyLib.AccessTools.Constructor(originalType, parameters)
			: HarmonyLib.AccessTools.Method(originalType, originalMethod, parameters);

		if (original == null)
		{
			AddConsoleLog(string.Format(Properties.Strings.ErrorCannotFindOriginalMethodFormat, $"{originalType}.{originalMethod ?? "ctor"}").Red());
			return;
		}

		var prefix = GetTargetMethod(newPrefixMethod, Properties.Strings.ErrorCannotFindPrefixMethodFormat);
		var postfix = GetTargetMethod(newPostfixMethod, Properties.Strings.ErrorCannotFindPostfixMethodFormat);

		if (prefix == null && postfix == null)
			return;

		harmony.Patch(original, prefix: prefix, postfix: postfix);
#if DEBUG
		AddConsoleLog(string.Format(Properties.Strings.DebugPatchedMethodFormat, $"{originalType}.{originalMethod}", $"{GetType()}.{newPrefixMethod ?? newPostfixMethod}"));
#endif
	}

	private HarmonyLib.HarmonyMethod? GetTargetMethod(string? methodName, string errorFormat)
	{
		if (methodName == null)
			return null;

		var method = HarmonyLib.AccessTools.Method(GetType(), methodName);
		if (method == null)
		{
			AddConsoleLog(string.Format(errorFormat, methodName).Red());
			return null;
		}

		return new HarmonyLib.HarmonyMethod(method);
	}

	public void HarmonyPrefix(HarmonyLib.Harmony harmony, Type originalType, string originalMethod, string newMethod, Type[]? parameters = null)
	{
		HarmonyDispatch(harmony, originalType, originalMethod, newPrefixMethod: newMethod, newPostfixMethod: null, parameters);
	}

	public void HarmonyConstructorPrefix(HarmonyLib.Harmony harmony, Type originalType, string newMethod, Type[]? parameters)
	{
		HarmonyDispatch(harmony, originalType, null, newPrefixMethod: newMethod, newPostfixMethod: null, parameters);
	}

	public void HarmonyPostfix(HarmonyLib.Harmony harmony, Type originalType, string originalMethod, string newMethod, Type[]? parameters = null)
	{
		HarmonyDispatch(harmony, originalType, originalMethod, newPrefixMethod: null, newPostfixMethod: newMethod, parameters);
	}

	protected void AddConsoleLog(string log)
	{
		if (PreloaderUI.Instantiated)
			ConsoleScreen.Log(log);
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override ETranslateResult TranslateCommand(ECommand command)
	{
		return ETranslateResult.Ignore;
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override void TranslateAxes(ref float[] axes)
	{
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override ECursorResult ShouldLockCursor()
	{
		return ECursorResult.Ignore;
	}
}
