namespace HowToFishSaveEditor;

// All values below were extracted from the game files (Assembly-CSharp.dll + Unity assets, game build Unity 6000.4.4f1).

public enum ItemCategory { Fish, EuroFish, Boss, Creature, Meat, Weapon, Melee, Tool, Trophy, Misc }

public sealed class ItemDef
{
    public byte Id { get; init; }
    public string Name { get; init; }
    public ItemCategory Category { get; init; }
    public string[] Skins { get; init; }
    public int[] SkinRarity { get; init; }
    public int BulletLevels { get; init; }
    public int QuiverLevels { get; init; }

    public bool HasSkins => Skins is { Length: > 1 };
    public bool InJournal => Category is ItemCategory.Fish or ItemCategory.EuroFish or ItemCategory.Boss or ItemCategory.Creature;
    public bool IsCreature => InJournal || Category == ItemCategory.Meat || Id == 56;
    public bool IsGun => BulletLevels > 0;
    public bool IsMelee => Category == ItemCategory.Melee;
    public string CategoryName => GameData.CategoryName(Category);

    public string SkinLabel(int i)
    {
        if (Skins == null || i < 0 || i >= Skins.Length) return $"Skin {i}";
        return $"{Skins[i]}  ({GameData.RarityName(SkinRarity[i])})";
    }
}

public static class GameData
{
    public const int StartingSlots = 3;
    public const int MaxExtraSlots = 5;          // PlayerInventory._extraSlotCosts = [5,10,25,50,100]
    public const int MaxWishbone = 5;            // 6 wishbone upgrades
    public const int MaxSharpness = 15;          // highest SharpnessPurchasable
    public const int IslandCount = 5;
    public const byte BoatSkinItemId = 255;

    public static string[] Islands => Enumerable.Range(1, IslandCount).Select(i => L.T($"Island {i}", $"Insel {i}")).ToArray();
    public static string[] Motors => new[] { L.T("Default motor", "Standard-Motor"), L.T("Motor level 1", "Motor Stufe 1"), L.T("Motor level 2 (max)", "Motor Stufe 2 (max)") };
    public static readonly string[] Difficulties = { "Easy", "Default", "Hard" };
    public static string[] Sights => new[] { L.T("Default sight", "Standard-Visier"), L.T("Red dot sight", "Red-Dot-Visier"), L.T("Sniper scope", "Sniper-Zielfernrohr") };
    public static string[] Barrels => new[] { L.T("Default barrel", "Standard-Lauf"), L.T("Compensator", "Kompensator"), L.T("Suppressor", "Schalldämpfer") };

    // Order = GameInfo._allBaits (empty bait first, then sorted by cost). Index = OwnedBaits index.
    public static readonly (string Name, int Cost)[] Baits =
    {
        ("(Empty)", 0),
        ("Fish Bucket", 0),
        ("Testing", 0),
        ("Hot Dog", 1),
        ("Leech Bait", 1),
        ("Beginner Lure", 3),
        ("Empty Beer Can", 8),
        ("Standard Lure", 15),
        ("Beginner Boss Lure", 40),
        ("Professional Lure", 50),
        ("Carrot", 100),
        ("Standard Boss Lure", 280),
        ("Coconut", 350),
        ("Scientific Lure", 500),
        ("Professional Boss Lure", 1200),
        ("Scientific Boss Lure", 5800),
        ("Crab Carcass", 10000),
    };

    public static readonly string[] BoatSkins = { "Default", "Winter", "Red", "Orange", "Blue", "Camouflage", "Emerald", "Ruby", "Fire", "Polka", "Blue Fade", "Rainbow", "Gold", "Diamond" };
    public static readonly int[] BoatSkinRarity = { 0, 1, 1, 1, 1, 2, 2, 2, 0, 2, 2, 3, 3, 3 };

    public static readonly ItemDef[] Items = Build();

    private static readonly Dictionary<byte, ItemDef> ById = Items.ToDictionary(i => i.Id);

    public static ItemDef Item(byte id) => ById.TryGetValue(id, out var d) ? d : null;

    public static string ItemName(long id) => id is >= 0 and <= 255 && Item((byte)id) is { } d ? d.Name : L.T($"Unknown [{id}]", $"Unbekannt [{id}]");

    public static string RarityName(int r) => r switch { 1 => "Common", 2 => "Rare", 3 => "Legendary", _ => "Default" };

    public static string CategoryName(ItemCategory c) => c switch
    {
        ItemCategory.Fish => L.T("Fish", "Fisch"),
        ItemCategory.EuroFish => L.T("Euro fish", "Euro-Fisch"),
        ItemCategory.Boss => "Boss",
        ItemCategory.Creature => L.T("Creature", "Kreatur"),
        ItemCategory.Meat => L.T("Meat", "Fleisch"),
        ItemCategory.Weapon => L.T("Weapon", "Waffe"),
        ItemCategory.Melee => L.T("Melee", "Nahkampf"),
        ItemCategory.Tool => L.T("Tool", "Werkzeug"),
        ItemCategory.Trophy => L.T("Trophy", "Trophäe"),
        _ => L.T("Misc", "Sonstiges"),
    };

    private static ItemDef[] Build()
    {
        var l = new List<ItemDef>();
        void A(byte id, string name, ItemCategory c) => l.Add(new ItemDef { Id = id, Name = name, Category = c });
        void S(byte id, string name, ItemCategory c, string skins, int[] rarity, int bullets = 0, int quiver = 0) =>
            l.Add(new ItemDef { Id = id, Name = name, Category = c, Skins = skins.Split('|'), SkinRarity = rarity, BulletLevels = bullets, QuiverLevels = quiver });

        // Fish
        A(0, "Angelfish", ItemCategory.Fish); A(1, "Anglerfish", ItemCategory.Fish); A(2, "Bass", ItemCategory.Fish);
        A(3, "Blobfish", ItemCategory.Fish); A(4, "Bluegill", ItemCategory.Fish); A(16, "Bowlfish", ItemCategory.Fish);
        A(17, "Catfish", ItemCategory.Fish); A(19, "Clownfish", ItemCategory.Fish); A(20, "Cod", ItemCategory.Fish);
        A(22, "Dripper", ItemCategory.Fish); A(23, "Eel", ItemCategory.Fish); A(24, "Flying Fish", ItemCategory.Fish);
        A(26, "Gar", ItemCategory.Fish); A(27, "Goby", ItemCategory.Fish); A(28, "Goldfish", ItemCategory.Fish);
        A(29, "Halibut", ItemCategory.Fish); A(33, "Mackerel", ItemCategory.Fish); A(34, "Needlefish", ItemCategory.Fish);
        A(35, "Oarfish", ItemCategory.Fish); A(36, "Parrotfish", ItemCategory.Fish); A(37, "Perch", ItemCategory.Fish);
        A(38, "Pike", ItemCategory.Fish); A(39, "Piranha", ItemCategory.Fish); A(40, "Red Snapper", ItemCategory.Fish);
        A(41, "Salmon", ItemCategory.Fish); A(42, "Sea Urchin", ItemCategory.Fish); A(44, "Seahorse", ItemCategory.Fish);
        A(45, "Sengarat", ItemCategory.Fish); A(46, "Shrimp", ItemCategory.Fish); A(47, "Stonefish", ItemCategory.Fish);
        A(48, "Superdwarf Fish", ItemCategory.Fish); A(49, "Tigerfish", ItemCategory.Fish); A(50, "Triggerfish", ItemCategory.Fish);
        A(51, "Voxel Fish", ItemCategory.Fish); A(52, "Yellow Boxfish", ItemCategory.Fish); A(80, "BingBong", ItemCategory.Fish);
        A(99, "Dogfish", ItemCategory.Fish); A(100, "Tripod Fish", ItemCategory.Fish); A(101, "Koi Fish", ItemCategory.Fish);
        A(102, "Carp", ItemCategory.Fish);
        // Euro fish
        A(88, "Squid", ItemCategory.EuroFish); A(89, "Starfish", ItemCategory.EuroFish); A(90, "Sponge", ItemCategory.EuroFish);
        A(94, "Tench", ItemCategory.EuroFish); A(95, "Alaska Pollock", ItemCategory.EuroFish); A(96, "Mahi-Mahi", ItemCategory.EuroFish);
        A(97, "Amberjack", ItemCategory.EuroFish); A(98, "Char", ItemCategory.EuroFish);
        // Bosses
        A(5, "Albatross", ItemCategory.Boss); A(6, "Blue Shark", ItemCategory.Boss); A(7, "Bowhead Whale", ItemCategory.Boss);
        A(8, "The Old Pike", ItemCategory.Boss); A(9, "Giant Piranha", ItemCategory.Boss); A(10, "Goblin Shark", ItemCategory.Boss);
        A(11, "Mutated Bowhead Whale", ItemCategory.Boss); A(12, "Pufferfish", ItemCategory.Boss); A(13, "Spidercrab", ItemCategory.Boss);
        A(14, "Sunfish", ItemCategory.Boss); A(15, "Tuna", ItemCategory.Boss); A(91, "Manta", ItemCategory.Boss);
        // Other creatures
        A(18, "Clam", ItemCategory.Creature); A(21, "Brown Crab", ItemCategory.Creature); A(25, "Foot Snail", ItemCategory.Creature);
        A(31, "Leech", ItemCategory.Creature); A(32, "Lobster", ItemCategory.Creature); A(43, "Seagull", ItemCategory.Creature);
        A(79, "Rock Crab", ItemCategory.Creature);
        // Meat
        A(81, "Whale Meat", ItemCategory.Meat); A(82, "Crab Meat", ItemCategory.Meat); A(83, "Pufferfish Meat", ItemCategory.Meat);
        A(84, "Albatross Meat", ItemCategory.Meat); A(85, "Piranha Meat", ItemCategory.Meat);
        // Guns (13 bullet upgrades, bow 11 + 7 quiver upgrades)
        S(54, "Assault Rifle", ItemCategory.Weapon, "Default|White|Orange|Blue|Tiger|Hazard|Wood|Emerald|Ruby|Pink|Cow|Rainbow|Diamond|Gold", new[] { 0, 1, 0, 0, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3 }, 13);
        S(66, "Pistol", ItemCategory.Weapon, "Default|Wood|Pink|White|Blaze|Bluze|Missing|Emerald|Black and White|Inverted Diamond|Galaxy|Ruby|Diamond|Gold", new[] { 0, 1, 1, 1, 1, 1, 0, 2, 2, 2, 2, 2, 3, 3 }, 13);
        S(68, "Shotgun", ItemCategory.Weapon, "Default|Orange|Redwood|Greyscale|Hazard|Wood|Bronze|Emerald|Ruby|GoldStriped|Rainbow|Diamond|Gold", new[] { 0, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3 }, 13);
        S(69, "SMG", ItemCategory.Weapon, "Default|White|Blue|Pink|Red|Brown|Hazard|Wood|Emerald|Ruby|Purple|Tiger|Rainbow|Diamond|Gold|Cow", new[] { 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 2 }, 13);
        S(70, "Sniper Rifle", ItemCategory.Weapon, "Default|Wood|Brown|Light Gray|Winter|Camouflage|Cow|Asiimov|Blaze|Sponge|Emerald|Ruby|Tiger|Galaxy|Gold|Rainbow", new[] { 0, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 2, 2, 2, 3, 3 }, 13);
        S(86, "Bow", ItemCategory.Weapon, "Default|Blue|Red|Cow|Hazard|Emerald|Ruby|Rainbow|Gold|Diamond|White|HalfGold", new[] { 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 1, 2 }, 11, 7);
        // Melee
        S(57, "Brass Knuckles", ItemCategory.Melee, "Default|Camouflage|Wood|Emerald|Polka|Hazard|Gold|Chess|Ruby|Rainbow|Diamond", new[] { 0, 2, 2, 2, 1, 2, 3, 1, 2, 3, 3 });
        S(58, "Brass Knuckles (Packed Backup)", ItemCategory.Melee, "Default|Camouflage|Wood|Emerald|Polka|Hazard|Gold|Chess|Ruby|Rainbow|Diamond", new[] { 0, 2, 2, 2, 1, 2, 3, 1, 2, 3, 3 });
        S(63, "Knife", ItemCategory.Melee, "Default|Camouflage|Chess|Tiger|Blaze|Emerald|Wood|Polka|Hazard|Rainbow|Galaxy|Missing|Ruby|Diamond|Gold", new[] { 0, 1, 1, 1, 2, 2, 2, 2, 1, 2, 2, 1, 2, 3, 3 });
        S(64, "Knife (Packed Backup)", ItemCategory.Melee, "Default|Camouflage|Chess|Tiger|Blaze|Emerald|Wood|Polka|Hazard|Rainbow|Galaxy|Missing|Ruby|Diamond|Gold", new[] { 0, 1, 1, 1, 2, 2, 2, 2, 1, 2, 2, 1, 2, 3, 3 });
        // Tools
        S(61, "Fishing Rod", ItemCategory.Tool, "Default|Orange|Winter|Cow|Hazard|Asiimov|Wood|Camouflage|Emerald|Ruby|Missing|Galaxy|Gold|Diamond", new[] { 0, 1, 1, 1, 1, 2, 1, 2, 2, 2, 2, 3, 0, 3 });
        A(59, "Crab Fishing Rod", ItemCategory.Tool); A(60, "Dynamite", ItemCategory.Tool); A(62, "Disc", ItemCategory.Tool);
        A(65, "Map", ItemCategory.Tool); A(67, "Radio", ItemCategory.Tool); A(87, "Ground Bait", ItemCategory.Tool);
        A(55, "Bad Ball", ItemCategory.Tool); A(71, "Snowball (1)", ItemCategory.Tool); A(72, "Snowball (2)", ItemCategory.Tool);
        A(73, "Snowball (3)", ItemCategory.Tool);
        // Trophies
        A(74, "Albatross Head", ItemCategory.Trophy); A(75, "Giant Piranha Skeleton", ItemCategory.Trophy);
        A(76, "Pufferfish Fin", ItemCategory.Trophy); A(77, "Spidercrab Shell", ItemCategory.Trophy); A(78, "Whale Fin", ItemCategory.Trophy);
        // Misc
        A(56, "Beer", ItemCategory.Misc); A(53, "Dead Player", ItemCategory.Misc);
        A(92, "Money in a Bottle", ItemCategory.Misc); A(93, "Barrel", ItemCategory.Misc);

        return l.OrderBy(i => i.Category).ThenBy(i => i.Name).ToArray();
    }
}
