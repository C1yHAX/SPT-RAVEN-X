using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal static class FeatureFactory
{
	private static readonly Lazy<Type[]> _types = new(() => [.. typeof(FeatureFactory)
		.Assembly
		.GetTypes()
		.Where(t => t.IsSubclassOf(typeof(Feature)) && !t.IsAbstract)]);

	private static Feature[] _features = [];
	private static ToggleFeature[] _toggleableFeatures = [];
	private static readonly Dictionary<Type, Feature> _featureIndex = [];

	public static Feature[] RegisterAllFeatures(GameObject gameObject)
	{
		var features = new List<Feature>(_types.Value.Length);
		_featureIndex.Clear();

		foreach (var type in _types.Value)
		{
			if (gameObject.GetOrAddComponent(type) is not Feature feature)
				continue;

			features.Add(feature);
			_featureIndex[type] = feature;
		}

		_features = features.ToArray();
		_toggleableFeatures = [.. _features.OfType<ToggleFeature>()];
		return _features;
	}

	public static Type[] GetAllFeatureTypes()
	{
		return _types.Value;
	}

	public static T? GetFeature<T>() where T : Feature
	{
		return _featureIndex.TryGetValue(typeof(T), out var feature) ? (T)feature : null;
	}

	public static Feature[] GetAllFeatures()
	{
		return _features;
	}

	public static ToggleFeature[] GetAllToggleableFeatures()
	{
		return _toggleableFeatures;
	}
}
