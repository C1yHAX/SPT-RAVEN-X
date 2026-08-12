using EFT;

namespace RavenX;

public static class KnownTemplateIds
{

	public const string BuriedBarrelCache = "5d6d2bb386f774785b07a77a";
	public const string GroundCache = "5d6d2b5486f774785c2ba8ea";

	public const string Pockets = "557ffd194bdc2d28148b457f";
	public const string DefaultInventory = "55d7217a4bdc2d86028b456d";
	public const string BossContainer = "5c0a794586f77461c458f892";

	public const string AirDropCommon = "6223349b3136504a544d1608";
	public const string AirDropMedical = "622334c873090231d904a9fc";
	public const string AirDropSupply = "622334fa3136504a544d160c";
	public const string AirDropWeapon = "6223351bb5d97a7b2c635ca7";

	public const string RedSignalFlare = "624c09cfbc2e27219346d955";

	public static string DefaultInventoryLocalizedShortName = ((MongoID)DefaultInventory).LocalizedShortName();
}
