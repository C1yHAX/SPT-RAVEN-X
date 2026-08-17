using System;
using System.IO;
using System.Linq;
using RavenX.Features;
using EFT;

namespace RavenX;

internal static class Context
{
	public static string UserPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov");
	public static string ConfigFile => Path.Combine(UserPath, "ravenx.ini");

	private static Lazy<Feature[]> _features = NewFeatures();
	private static Lazy<ToggleFeature[]> _toggleableFeatures = NewToggleableFeatures();

	private static Lazy<Feature[]> NewFeatures() => new(() => [.. FeatureFactory.GetAllFeatures().OrderBy(f => f.Name)]);
	private static Lazy<ToggleFeature[]> NewToggleableFeatures() => new(() => [.. FeatureFactory.GetAllToggleableFeatures().OrderByDescending(f => f.Name)]);

	public static Lazy<Feature[]> Features => _features;
	public static Lazy<ToggleFeature[]> ToggleableFeatures => _toggleableFeatures;

	public static void ResetFeatureCache()
	{
		_features = NewFeatures();
		_toggleableFeatures = NewToggleableFeatures();
	}
}
