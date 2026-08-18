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

	// Set for things carried by someone who walks away. Positions are only collected
	// every few seconds, so without this the marker trails well behind a moving bot.
	public Transform? Follow { get; set; }
}
