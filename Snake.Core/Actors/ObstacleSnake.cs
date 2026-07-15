using SnakeGameEngine.AI;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine.Actors;

public class ObstacleSnake
{
    // While an EMP is active, every enemy falls back to blind wandering.
    private static readonly RandomBrain ScrambledBrain = new();

    private int survivalPerkCooldown;
    private bool ironShieldAvailable = true;

    public List<ObstacleInfo> Cells { get; }

    public Direction Direction { get; set; }

    public EnemyPersonality Personality { get; }

    public ISnakeBrain Brain { get; }

    // Perks the enemy has earned (loaded from its profile, extended by leveling up).
    public List<string> EnemyPerks { get; } = new();

    public int FoodEaten { get; private set; }

    public ObstacleSnake(List<ObstacleInfo> cells, Direction direction, EnemyPersonality personality, ISnakeBrain brain)
    {
        Cells = cells;
        Direction = direction;
        Personality = personality;
        Brain = brain;
        foreach (var cell in cells)
        {
            cell.FallbackColor = personality.FallbackColor;
        }
    }

    public bool HasPerk(string perkName)
    {
        return EnemyPerks.Contains(perkName);
    }

    // Iron Head / Ghost Phase style escapes: the snake survives a fatal move by staying put.
    public bool TryPerkSurvive()
    {
        if (HasPerk("Iron Head") && ironShieldAvailable)
        {
            ironShieldAvailable = false;
            return true;
        }

        if (HasPerk("Ghost Phase") && survivalPerkCooldown == 0)
        {
            survivalPerkCooldown = 100;
            return true;
        }

        return false;
    }

    // Asks the brain for a move. A brain returning no direction means the snake is boxed in;
    // a direction into a blocked cell is a fatal crash - dumb brains do that. Both set hasDied.
    // Eats (and removes) any food it steps on, growing by the personality's growth rate.
    // Returns the vacated tail location so it can be repainted, or null if nothing was vacated.
    public Location? Move(GameState gameState, HashSet<(int X, int Y)> blockedCells, out bool hasDied)
    {
        hasDied = false;
        if (survivalPerkCooldown > 0)
        {
            survivalPerkCooldown--;
        }

        var brain = gameState.EmpTicksRemaining > 0 ? ScrambledBrain : Brain;
        var direction = brain.ChooseDirection(this, gameState, blockedCells);
        if (direction == null)
        {
            hasDied = true;
            return null;
        }

        var newHeadLocation = BrainHelper.GetNeighborLocation(Cells[0].Location, direction.Value);
        if (!BrainHelper.IsFree((newHeadLocation.X, newHeadLocation.Y), blockedCells))
        {
            hasDied = true;
            return null;
        }

        Direction = direction.Value;
        var vacatedTail = Cells[^1].Location;
        for (int i = Cells.Count - 1; i > 0; i--)
        {
            Cells[i].Location = Cells[i - 1].Location;
        }
        Cells[0].Location = newHeadLocation;

        // Viper's earned Poison Trail: it keeps dripping poison behind itself.
        if (HasPerk("Poison Trail") && gameState.TickNumber % 50 == 0)
        {
            gameState.PoisonClouds.Add(new PoisonCloud(new Location(vacatedTail.X, vacatedTail.Y), gameState.TickNumber + 80));
        }

        var eatenFood = gameState.Foods.FirstOrDefault(food => food.Location.Equals(newHeadLocation));
        if (eatenFood != null)
        {
            gameState.Foods.Remove(eatenFood);
            FoodEaten++;
            LearnFavoritePerkIfLeveled();
            for (int i = 0; i < Personality.GrowthPerFood; i++)
            {
                Cells.Add(new ObstacleInfo(vacatedTail) { FallbackColor = Personality.FallbackColor });
            }
            return null;
        }
        return vacatedTail;
    }

    private void LearnFavoritePerkIfLeveled()
    {
        if (FoodEaten >= Settings.Current.PointsPerLevel
            && Personality.FavoritePerk != "None"
            && !HasPerk(Personality.FavoritePerk))
        {
            EnemyPerks.Add(Personality.FavoritePerk);
        }
    }
}
