using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX;

public class Loader
{
	private static GameObject HookObject
	{
		get
		{
			var result = GameObject.Find("Application (Main Client)");
			if (result != null)
				return result;

			result = new GameObject(nameof(Loader));
			Object.DontDestroyOnLoad(result);
			return result;
		}
	}

	public static void Load()
	{
		FeatureFactory.RegisterAllFeatures(HookObject);
	}
}
