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
