using System.Collections.Generic;
using System.IO;
using System.Linq;
using RavenX.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class Hotspot
{
	public string Map { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }

	[JsonIgnore]
	public Vector3 Position
	{
		get => new(X, Y, Z);
		set { X = value.x; Y = value.y; Z = value.z; }
	}
}

[UsedImplicitly]
internal class Hotspots : Feature
{
	public override string Name => "hotspots";
	public override string Description => "Saved positions per map.";

	private static string FilePath => Path.Combine(Context.UserPath, "hotspots.json");

	private List<Hotspot>? _all;
	private bool _loadedSafely = true;

	internal List<Hotspot> All => _all ??= Load();
	internal string? LastError { get; private set; }

	internal static string CurrentMap
	{
		get
		{
			var scene = SceneManager.GetActiveScene();
			return scene.isLoaded ? scene.name : string.Empty;
		}
	}

	internal IEnumerable<Hotspot> ForCurrentMap()
	{
		var map = CurrentMap;
		return map.Length == 0
			? []
			: All.Where(h => string.Equals(h.Map, map, System.StringComparison.OrdinalIgnoreCase));
	}

	internal bool Add(string name)
	{
		var player = GameState.Current?.LocalPlayer;
		var map = CurrentMap;

		if (!player.IsValid() || map.Length == 0 || name.Trim().Length == 0)
			return false;

		var all = All;
		if (!_loadedSafely)
			return false;

		var hotspot = new Hotspot { Map = map, Name = name.Trim(), Position = player.Transform.position };
		all.Add(hotspot);
		if (Save())
			return true;

		all.Remove(hotspot);
		return false;
	}

	internal bool Remove(Hotspot hotspot)
	{
		var all = All;
		if (!_loadedSafely)
			return false;

		var index = all.IndexOf(hotspot);
		if (index < 0)
			return false;

		all.RemoveAt(index);
		if (Save())
			return true;

		all.Insert(index, hotspot);
		return false;
	}

	internal static bool TeleportTo(Hotspot hotspot)
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return false;

		player.Teleport(hotspot.Position + Vector3.up * 0.3f, false);
		return true;
	}

	private List<Hotspot> Load()
	{
		try
		{
			if (!File.Exists(FilePath))
				return [];

			var result = JsonConvert.DeserializeObject<List<Hotspot>>(File.ReadAllText(FilePath)) ?? [];
			LastError = null;
			_loadedSafely = true;
			return result;
		}
		catch (System.Exception ex)
		{
			LastError = $"Unable to read hotspots.json: {ex.Message}";
			_loadedSafely = false;
			return [];
		}
	}

	internal bool Save()
	{
		if (!_loadedSafely)
			return false;

		try
		{
			Directory.CreateDirectory(Context.UserPath);
			Configuration.ConfigurationManager.WriteAtomic(FilePath, JsonConvert.SerializeObject(All, Formatting.Indented));
			LastError = null;
			return true;
		}
		catch (System.Exception ex)
		{
			LastError = $"Unable to save hotspots.json: {ex.Message}";
			return false;
		}
	}
}
