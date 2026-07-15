namespace SnakeGameEngine;

// Raised by the game world; each client decides how to play them.
public enum SoundEvent
{
    FoodEaten,
    LuckyFood,
    PerkGained,
    ShieldAbsorbed,
    EnemyDied,
    PlayerDied,
    Win,
    BirdChirp,
    BirdCaught
}
