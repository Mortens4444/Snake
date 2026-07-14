namespace SnakeGameEngine;

public static class Constants
{
    public const int MaxX = 120;

    // Playfield height; the row below it holds the status line, so both fit a 30-row console.
    public const int MaxY = 29;

    public const int HalfOfMaxX = MaxX / 2;

    public const int HalfOfMaxY = MaxY / 2;

    public const int TargetSnakeLength = 50;

    public const int InitialTickMilliseconds = 100;

    public const int MinimumTickMilliseconds = 50;

    public const int SpeedUpPerBodyPart = 1;

    public const int StatusLineY = MaxY;

    public const int ObstacleCount = 8;

    public const int ObstacleMinLength = 3;

    public const int ObstacleMaxLength = 8;

    // No obstacle may spawn this close to the snake's starting position.
    public const int ObstacleFreeZoneRadius = 6;

    // Obstacles move at half the player's speed.
    public const int ObstacleMoveEveryNthTick = 2;

    public const int ObstacleTurnChancePercent = 20;
}
