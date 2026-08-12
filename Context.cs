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

	public static Lazy<Feature[]> Features => new(() => [.. FeatureFactory.GetAllFeatures().OrderBy(f => f.Name)]);
	public static Lazy<ToggleFeature[]> ToggleableFeatures => new(() => [.. FeatureFactory.GetAllToggleableFeatures().OrderByDescending(f => f.Name)]);
}
