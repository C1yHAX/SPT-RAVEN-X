using System.Collections.Generic;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Chams : ToggleFeature
{
	public override string Name => "chams";
	public override string Description => "Colour characters through walls.";

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool ShowUsec { get; set; } = true;

	[ConfigurationProperty(Order = 10)]
	public bool ShowBear { get; set; } = true;

	[ConfigurationProperty(Order = 10)]
	public bool ShowCultist { get; set; } = true;

	[ConfigurationProperty(Order = 10)]
	public bool ShowRaider { get; set; } = true;

	[ConfigurationProperty(Order = 20)]
	public Color UsecVisibleColor { get; set; } = new(0.16f, 0.78f, 0.36f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color UsecOccludedColor { get; set; } = new(0.10f, 0.45f, 0.22f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color BearVisibleColor { get; set; } = new(0.24f, 0.55f, 0.98f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color BearOccludedColor { get; set; } = new(0.13f, 0.30f, 0.60f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color CultistVisibleColor { get; set; } = new(0.79f, 0.31f, 0.93f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color CultistOccludedColor { get; set; } = new(0.45f, 0.16f, 0.55f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color RaiderVisibleColor { get; set; } = new(0.98f, 0.55f, 0.20f, 0.85f);

	[ConfigurationProperty(Order = 20)]
	public Color RaiderOccludedColor { get; set; } = new(0.60f, 0.32f, 0.10f, 0.85f);

	[ConfigurationProperty(Order = 11)]
	public bool ShowScav { get; set; } = true;

	[ConfigurationProperty(Order = 12)]
	public bool ShowBoss { get; set; } = true;


	[ConfigurationProperty(Order = 22)]
	public Color ScavVisibleColor { get; set; } = new(0.93f, 0.77f, 0.20f, 0.85f);

	[ConfigurationProperty(Order = 23)]
	public Color ScavOccludedColor { get; set; } = new(0.80f, 0.45f, 0.12f, 0.85f);

	[ConfigurationProperty(Order = 24)]
	public Color BossVisibleColor { get; set; } = new(0.79f, 0.31f, 0.93f, 0.85f);

	[ConfigurationProperty(Order = 25)]
	public Color BossOccludedColor { get; set; } = new(0.55f, 0.18f, 0.70f, 0.85f);

	[ConfigurationProperty(Order = 30)]
	public float Opacity { get; set; } = 0.85f;

	[ConfigurationProperty(Order = 31)]
	public float MaximumDistance { get; set; } = 0f;

	[ConfigurationProperty(Browsable = false)]
	public List<RoleSetting> RoleColors { get; set; } = [];

	private Dictionary<string, RoleSetting>? _roleIndex;

	[ConfigurationProperty(Order = 80)]
	public bool ShowCorpses { get; set; } = false;

	[ConfigurationProperty(Order = 81)]
	public Color CorpseColor { get; set; } = new(0.85f, 0.35f, 0.35f, 0.85f);

	[ConfigurationProperty(Order = 82)]
	public bool ShowLoot { get; set; } = false;

	[ConfigurationProperty(Order = 83)]
	public Color LootColor { get; set; } = new(0.30f, 0.85f, 0.90f, 0.85f);

	private readonly Dictionary<int, Painted> _painted = [];
	private readonly List<Renderer> _rendererBuffer = [];
	private readonly List<int> _stale = [];
	private readonly HashSet<int> _seen = [];

	private Material? _visibleMaterial;
	private Material? _occludedMaterial;

	private sealed class Painted
	{
		public Renderer?[] Renderers = [];
		public Material[][] Original = [];
	}

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
		{
			RestoreAll();
			return;
		}

		if (!EnsureMaterials())
			return;

		var origin = player!.Transform.position;
		_seen.Clear();

		foreach (var hostile in state.Hostiles)
		{
			if (hostile == null || !hostile.IsAlive())
				continue;

			if (!WantsChams(hostile))
				continue;

			if (TooFar(origin, hostile.Transform.position))
				continue;

			var key = hostile.GetInstanceID();

			if (!_painted.ContainsKey(key) && !CapturePlayer(hostile))
				continue;

			Colors(hostile, out var visible, out var occluded);
			Paint(key, visible, occluded);
			_seen.Add(key);
		}

		if (ShowCorpses || ShowLoot)
			PaintWorld(origin);

		_stale.Clear();
		foreach (var entry in _painted)
		{
			if (!_seen.Contains(entry.Key))
				_stale.Add(entry.Key);
		}

		foreach (var lost in _stale)
			Restore(lost);
	}

	private bool TooFar(Vector3 origin, Vector3 position) => MaximumDistance > 0f && Vector3.Distance(origin, position) > MaximumDistance;

	private void PaintWorld(Vector3 origin)
	{
		var world = Comfort.Common.Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var lootItems = world.LootItems;

		for (var i = 0; i < lootItems.Count; i++)
		{
			var lootItem = lootItems.GetByIndex(i);
			if (!lootItem.IsValid())
				continue;

			var isCorpse = lootItem is EFT.Interactive.Corpse;

			if (isCorpse ? !ShowCorpses : !ShowLoot)
				continue;

			if (TooFar(origin, lootItem.transform.position))
				continue;

			var key = lootItem.GetInstanceID();

			if (!_painted.ContainsKey(key) && !CaptureObject(lootItem.gameObject))
				continue;

			var visible = isCorpse ? CorpseColor : LootColor;
			var occluded = RoleCatalog.OccludedFrom(visible);

			visible.a = Opacity;
			occluded.a = Opacity;

			Paint(key, visible, occluded);
			_seen.Add(key);
		}
	}

	protected override void UpdateWhenDisabled()
	{
		RestoreAll();
	}

	public RoleSetting RoleFor(string key) => RoleCatalog.Resolve(RoleColors, ref _roleIndex, key);

	private RoleSetting RoleFor(Player hostile) => RoleFor(RoleCatalog.KeyOf(hostile));

	private bool WantsChams(Player hostile) => RoleFor(hostile).Enabled;

	private void Colors(Player hostile, out Color visible, out Color occluded)
	{
		var role = RoleFor(hostile);

		visible = role.Visible;
		occluded = role.Occluded;

		visible.a = Opacity;
		occluded.a = Opacity;
	}

	private void Paint(int key, Color visible, Color occluded)
	{
		if (!_painted.TryGetValue(key, out var painted))
		{
			painted = new Painted
			{
				Renderers = [.. _rendererBuffer]
			};

			painted.Original = new Material[painted.Renderers.Length][];

			var owned = false;

			for (var i = 0; i < painted.Renderers.Length; i++)
			{
				var renderer = painted.Renderers[i];
				if (renderer == null)
				{
					painted.Original[i] = [];
					continue;
				}

				var materials = renderer.sharedMaterials;

				if (AlreadyPainted(materials))
				{
					painted.Renderers[i] = null;
					painted.Original[i] = [];
					continue;
				}

				painted.Original[i] = materials;
				owned = true;
			}

			if (!owned)
				return;

			_painted[key] = painted;
		}

		_visibleMaterial!.color = visible;
		_occludedMaterial!.color = occluded;

		foreach (var renderer in painted.Renderers)
		{
			if (renderer == null)
				continue;

			renderer.sharedMaterials = [_visibleMaterial, _occludedMaterial];
		}
	}

	private bool CapturePlayer(Player hostile)
	{
		_rendererBuffer.Clear();

		var body = hostile.PlayerBody;
		if (body == null)
			return false;

		try
		{
			body.GetRenderersNonAlloc(_rendererBuffer);
		}
		catch
		{

			return false;
		}

		return _rendererBuffer.Count > 0;
	}

	private bool CaptureObject(GameObject? source)
	{
		_rendererBuffer.Clear();

		if (source == null)
			return false;

		source.GetComponentsInChildren(true, _rendererBuffer);
		return _rendererBuffer.Count > 0;
	}

	private bool AlreadyPainted(Material[] materials)
	{
		foreach (var material in materials)
		{
			if (ReferenceEquals(material, _visibleMaterial) || ReferenceEquals(material, _occludedMaterial))
				return true;
		}

		return false;
	}

	private void Restore(int key)
	{
		if (!_painted.TryGetValue(key, out var painted))
			return;

		for (var i = 0; i < painted.Renderers.Length; i++)
		{
			var renderer = painted.Renderers[i];
			if (renderer != null)
				renderer.sharedMaterials = painted.Original[i];
		}

		_painted.Remove(key);
	}

	private void RestoreAll()
	{
		if (_painted.Count == 0)
			return;

		_stale.Clear();
		_stale.AddRange(_painted.Keys);

		foreach (var hostile in _stale)
			Restore(hostile);
	}

	private bool EnsureMaterials()
	{
		if (_visibleMaterial != null && _occludedMaterial != null)
			return true;

		var shader = Shader.Find("Hidden/Internal-Colored");
		if (shader == null)
			return false;

		_visibleMaterial = CreateMaterial(shader, "RavenChamVisible", CompareFunction.LessEqual);
		_occludedMaterial = CreateMaterial(shader, "RavenChamOccluded", CompareFunction.Greater);
		return true;
	}

	private static Material CreateMaterial(Shader shader, string name, CompareFunction zTest)
	{
		var material = new Material(shader)
		{
			name = name,
			hideFlags = HideFlags.HideAndDontSave,
			renderQueue = 3000
		};

		material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
		material.SetInt("_Cull", (int)CullMode.Back);
		material.SetInt("_ZWrite", 0);
		material.SetInt("_ZTest", (int)zTest);

		return material;
	}
}
