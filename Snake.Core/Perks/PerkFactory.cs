namespace SnakeGameEngine.Perks;

public static class PerkFactory
{
    public static List<Perk> CreateAll()
    {
        return new List<Perk>
        {
            new IronHeadPerk(),
            new MetabolismPerk(),
            new DoubleHarvestPerk(),
            new HandbrakePerk(),
            new BerserkPerk(),
            new SpikyTailPerk(),
            new PoisonTrailPerk(),
            new TimeWarpPerk(),
            new EmpPerk(),
            new AppleMagnetPerk(),
            new GhostPhasePerk(),
            new TailWhipPerk(),
            new AmphibiousPerk(),
            new WoodpeckerPerk(),
            new ChameleonPerk()
        };
    }

    public static Perk? CreateByName(string name)
    {
        return CreateAll().FirstOrDefault(perk => perk.Name == name);
    }

    public static List<Perk> GetRandomChoices(List<Perk> ownedPerks, int count)
    {
        return CreateAll()
            .Where(perk => ownedPerks.All(ownedPerk => ownedPerk.Name != perk.Name))
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();
    }
}
