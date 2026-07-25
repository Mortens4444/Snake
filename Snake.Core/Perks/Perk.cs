namespace SnakeGameEngine.Perks;

// Base class for every perk. Passive perks override the Modify*/On* hooks;
// active perks also set ActivationKey and react in OnActivate.
public abstract class Perk
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    // Set for active perks; passive perks leave it null.
    public virtual ConsoleKey? ActivationKey => null;

    public virtual int CooldownTicks => 0;

    public int CooldownRemaining { get; set; }

    public bool IsReady => CooldownRemaining <= 0;

    // Set once, when a perk instance is resolved into GameState.GuestPerks, so its hooks know
    // to target the guest snake/timers instead of the host's.
    public bool IsGuestOwned { get; set; }

    // Perks that only work through a mechanism the guest doesn't have (see the specific
    // override for why) opt out of the guest's choice pool via this.
    public virtual bool IsEligibleForGuest => true;

    public void TryActivate(GameState gameState)
    {
        if (ActivationKey != null && IsReady)
        {
            gameState.HasUsedActivePerk = true;
            OnActivate(gameState);
            CooldownRemaining = CooldownTicks;
        }
    }

    protected virtual void OnActivate(GameState gameState)
    {
    }

    public virtual void OnTick(GameState gameState)
    {
    }

    public virtual void OnLevelUp(GameState gameState)
    {
    }

    public virtual int ModifyPoints(int points, GameState gameState) => points;

    public virtual int ModifyGrowth(int growth, GameState gameState) => growth;

    public virtual int ModifyTickMilliseconds(int milliseconds, GameState gameState) => milliseconds;
}
