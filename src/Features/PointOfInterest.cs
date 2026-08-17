using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal struct PointOfInterest
{
	public string Name { get; set; }
	public string? Owner { get; set; }
	public Vector3 Position { get; set; }
	public Color Color { get; set; }
}
