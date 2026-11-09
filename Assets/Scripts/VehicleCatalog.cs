using UnityEngine;

/// <summary>
/// Name and price of every vehicle. Speeds and the harvest bonus are on the prefabs (Assets/Prefabs/Van.prefab,
/// Harvester.prefab, Tractor.prefab); vehicles are red, there is no colour choice.
/// </summary>
public static class VehicleCatalog
{
    public struct Info
    {
        public VehicleKind kind;
        public string name;
        public int price;
    }

    // 2D game: the van and the harvester cost 5,000 each.
    private static readonly Info[] All =
    {
        new() { kind = VehicleKind.Tractor, name = "Tractor", price = 0 },
        new() { kind = VehicleKind.Van, name = "Van", price = 5000 },
        new() { kind = VehicleKind.Harvester, name = "Harvester", price = 5000 },
    };

    public static Info Get(VehicleKind kind) => All[(int)kind];
}
