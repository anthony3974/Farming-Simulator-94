using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Name, price, stats and dealer description of every vehicle. The dealer menu shows the name and the
/// description text below for each vehicle for sale. The models are prefabs (Assets/Prefabs/Van.prefab,
/// Harvester.prefab, Tractor.prefab); vehicles are red, there is no colour choice.
/// </summary>
public static class VehicleCatalog
{
    public struct Info
    {
        public VehicleKind kind;
        public string name;
        public int price;
        public float topSpeed;     // km/h on the speedometer (the tractor is 45; all cut 10%, owner 2026-11-05)
        public float harvestBonus; // 0.6 = 60% more harvest
        public string description;
    }

    // 2D game: the van and the harvester cost 5,000 each. Speeds keep the 2D ratios
    // (tractor 8, van 10, harvester 6), so the van is 25% faster and the harvester 25% slower.
    private static readonly Info[] All =
    {
        new() { kind = VehicleKind.Tractor, name = "Tractor", price = 0, topSpeed = 45, description = "Your starting vehicle." },
        new() { kind = VehicleKind.Van, name = "Van", price = 5000, topSpeed = 55.8f, description = "The fastest on the road." },
        new() { kind = VehicleKind.Harvester, name = "Harvester", price = 5000, topSpeed = 34.2f, harvestBonus = 0.6f, description = "Harvests 60% more from every field." },
    };

    public static Info Get(VehicleKind kind) => All[(int)kind];

    /// <summary>The vehicles sold at the dealer (everything but the starting tractor).</summary>
    public static IEnumerable<Info> ForSale
    {
        get { foreach (var info in All) if (info.price > 0) yield return info; }
    }
}
