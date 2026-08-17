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

	internal List<Hotspot> All => _all ??= Load();

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

		All.Add(new Hotspot { Map = map, Name = name.Trim(), Position = player.Transform.position });
		Save();
		return true;
	}

	internal void Remove(Hotspot hotspot)
	{
		All.Remove(hotspot);
		Save();
	}

	internal static bool TeleportTo(Hotspot hotspot)
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return false;

		player.Teleport(hotspot.Position + Vector3.up * 0.3f, false);
		return true;
	}

	private static List<Hotspot> Load()
	{
		try
		{
			if (!File.Exists(FilePath))
				return [];

			return JsonConvert.DeserializeObject<List<Hotspot>>(File.ReadAllText(FilePath)) ?? [];
		}
		catch
		{

			return [];
		}
	}

	internal void Save()
	{
		try
		{
			Directory.CreateDirectory(Context.UserPath);
			File.WriteAllText(FilePath, JsonConvert.SerializeObject(All, Formatting.Indented));
		}
		catch
		{

		}
	}
}
