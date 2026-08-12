using System.Reflection;
using EFT;

#nullable enable

namespace RavenX.Configuration;

internal class OrderedProperty(ConfigurationPropertyAttribute attribute, PropertyInfo property)
{
	public ConfigurationPropertyAttribute Attribute { get; } = attribute;
	public PropertyInfo Property { get; } = property;
}
