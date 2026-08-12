using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

public class RoleSetting
{
	public string Key { get; set; } = string.Empty;
	public bool Enabled { get; set; } = true;
	public Color Visible { get; set; } = Color.white;
	public Color Occluded { get; set; } = Color.gray;

	public RoleSetting()
	{
	}

	public RoleSetting(string key, Color visible, Color occluded)
	{
		Key = key;
		Visible = visible;
		Occluded = occluded;
	}
}
