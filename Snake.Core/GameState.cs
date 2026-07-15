using SnakeGameEngine.Actors;
using SnakeGameEngine.Moving;
using SnakeGameEngine.Perks;
using System.Diagnostics;

namespace SnakeGameEngine;

public sealed record PoisonCloud(Location Location, int ExpiresAtTick);

public sealed record ReplayCell(int X, int Y, char DisplayChar, (int R, int G, int B) Color);

// The whole game world and its tick logic. Never touches the console:
// it only mutates state and records vacated cells for the renderer to repaint.
public class GameState
{
    private readonly Stopwatch stopwatch = new();
    private int enemyMoveRound;

    public Snake PlayerSnake { get; } = new();

    // Second, network-controlled snake for LAN co-op. Null in single-player.
    public Snake? GuestSnake { get; private set; }

    public bool GuestAlive { get; private set; }

    public string? GuestKilledBy { get; private set; }

    public string? WinnerName { get; private set; }

    // Set by the host's network layer before each Tick(); consumed once, then reset.
    public GameAction PendingGuestAction { get; set; }

    public List<ObstacleSnake> EnemySnakes { get; }

    public List<ObstacleInfo> EnemyCells { get; }

    public List<FoodInfo> Foods { get; } = new();

    public Background Background { get; }

    public List<Location> VacatedLocations { get; } = new();

    public GameStatus Status { get; private set; } = GameStatus.Running;

    public string? PlayerKilledBy { get; private set; }

    public List<string> DeadEnemyNames { get; } = new();

    public int TickNumber { get; private set; }

    public int FoodsEaten { get; private set; }

    public bool HasUsedActivePerk { get; set; }

    // Progression
    public int Level { get; private set; } = 1;

    public int LevelPoints { get; private set; }

    public bool PendingPerkChoice { get; set; }

    public List<Perk> PlayerPerks { get; } = new();

    // Timed effects driven by perks
    public int GhostTicksRemaining { get; set; }

    public int EmpTicksRemaining { get; set; }

    public int TimeWarpTicksRemaining { get; set; }

    public int SlowdownTicksRemaining { get; set; }

    public int ShieldCharges { get; set; }

    public List<PoisonCloud> PoisonClouds { get; } = new();

    // Raised during Tick; the client drains and plays them.
    public List<SoundEvent> SoundEvents { get; } = new();

    // Ring buffer of lightweight draw snapshots for the death replay (every 2nd tick, ~10 seconds).
    public Queue<List<ReplayCell>> ReplayFrames { get; } = new();

    // The catchable bird flying across the field
    public Location? BirdLocation { get; private set; }

    private int birdStepX;
    private int nextBirdSpawnTick = -1;
    private string typedLetters = "";

    public TimeSpan Elapsed => stopwatch.Elapsed;

    public int Score => (int)Math.Round(PlayerSnake.SnakeBodyParts.Count * 100000 / stopwatch.Elapsed.TotalSeconds);

    public GameState()
    {
        Background = Background.Generate();
        EnemySnakes = ObstacleProducer.GetObstacles(Background);
        EnemyCells = EnemySnakes.SelectMany(enemySnake => enemySnake.Cells).ToList();
        Foods.Add(FoodProducer.GetFood(PlayerSnake, EnemyCells, Foods, Background));

        foreach (var perkName in PlayerProgress.Load().PerkNames)
        {
            var perk = PerkFactory.CreateByName(perkName);
            if (perk != null)
            {
                PlayerPerks.Add(perk);
            }
        }

        ScheduleNextBird();
        stopwatch.Start();
    }

    // Spawns the second, network-controlled snake for a LAN co-op session. Call once, before the
    // first Tick(); the offset keeps it inside the obstacle-free zone around the map center so it
    // never spawns inside a wall.
    public void EnableGuest()
    {
        var center = new Location(Settings.Current.MapWidth / 2, Settings.Current.MapHeight / 2);
        var guestHead = new Location(center.X - 4, center.Y - 3);
        GuestSnake = new Snake(guestHead);
        GuestAlive = true;
    }

    public void Pause()
    {
        stopwatch.Stop();
    }

    public void Resume()
    {
        stopwatch.Start();
    }

    public void Tick(GameAction action, ConsoleKey pressedKey = default)
    {
        VacatedLocations.Clear();
        TickTimedEffects(pressedKey);
        CheckCheatCodes(pressedKey);

        PlayerSnake.SetDirection(action);
        if (GuestSnake != null && GuestAlive)
        {
            GuestSnake.SetDirection(PendingGuestAction);
        }
        PendingGuestAction = GameAction.None;

        HandleEating();
        if (GuestSnake != null && GuestAlive)
        {
            HandleGuestEating();
        }
        if (Status != GameStatus.Running)
        {
            return;
        }

        var previousTail = PlayerSnake.Tail;
        VacatedLocations.Add(previousTail);
        PlayerSnake.Move();

        Location? guestPreviousTail = null;
        if (GuestSnake != null && GuestAlive)
        {
            guestPreviousTail = GuestSnake.Tail;
            VacatedLocations.Add(guestPreviousTail);
            GuestSnake.Move();
        }

        if (!TrySurviveCollisions(previousTail))
        {
            RecordReplayFrame();
            return;
        }

        if (GuestSnake != null && GuestAlive && guestPreviousTail != null)
        {
            TryResolveGuestCollisions();
        }

        TickNumber++;
        if (TimeWarpTicksRemaining == 0
            && (SlowdownTicksRemaining > 0 || TickNumber % Settings.Current.ObstacleMoveEveryNthTick == 0))
        {
            MoveEnemySnakes();
        }

        UpdateBird();

        if (TickNumber % 2 == 0)
        {
            RecordReplayFrame();
        }
    }

    public bool IsPoisoned(Location location)
    {
        return PoisonClouds.Any(cloud => cloud.Location.Equals(location));
    }

    public bool IsCellFreeForFood(Location location)
    {
        return location.X >= 0 && location.X < Settings.Current.MapWidth
            && location.Y >= 0 && location.Y < Settings.Current.MapHeight
            && !Background.IsCanopyAt(location)
            && !EnemyCells.Any(cell => cell.Location.Equals(location))
            && !PlayerSnake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(location))
            && !Foods.Any(food => food.Location.Equals(location));
    }

    // With the Chameleon perk, grass and canopy hide the player from the hunter brains.
    public bool IsPlayerHiddenFromHunters =>
        PlayerPerks.Any(perk => perk is ChameleonPerk)
        && (Background.IsGrassAt(PlayerSnake.Head) || Background.IsCanopyAt(PlayerSnake.Head));

    // The player's current speed: length ramp, perk modifiers, poison and water effects.
    public int GetTickMilliseconds()
    {
        var tick = Settings.Current.InitialTickMilliseconds
            - PlayerSnake.SnakeBodyParts.Count * Settings.Current.SpeedUpPerBodyPart;
        tick = Math.Max(Settings.Current.MinimumTickMilliseconds, tick);

        foreach (var perk in PlayerPerks)
        {
            tick = perk.ModifyTickMilliseconds(tick, this);
        }

        if (IsPoisoned(PlayerSnake.Head))
        {
            tick *= 2;
        }

        // The lake slows swimmers down - unless they are amphibious, who are faster in water.
        if (Background.IsWaterAt(PlayerSnake.Head))
        {
            tick = PlayerPerks.Any(perk => perk is AmphibiousPerk) ? tick * 8 / 10 : tick * 2;
        }
        return Math.Max(10, tick);
    }

    // Tail Whip: nearby enemies recoil, losing their front cells; short ones die outright.
    public void TailWhip()
    {
        foreach (var enemySnake in EnemySnakes.ToList())
        {
            var head = enemySnake.Cells[0].Location;
            var distance = Math.Abs(head.X - PlayerSnake.Head.X) + Math.Abs(head.Y - PlayerSnake.Head.Y);
            if (distance > 5)
            {
                continue;
            }

            if (enemySnake.Cells.Count <= 4)
            {
                KillEnemySnake(enemySnake);
                continue;
            }

            for (int i = 0; i < 2; i++)
            {
                var cell = enemySnake.Cells[0];
                enemySnake.Cells.RemoveAt(0);
                EnemyCells.Remove(cell);
                VacatedLocations.Add(cell.Location);
            }
        }
    }

    private void TickTimedEffects(ConsoleKey pressedKey)
    {
        if (GhostTicksRemaining > 0)
        {
            GhostTicksRemaining--;
        }
        if (EmpTicksRemaining > 0)
        {
            EmpTicksRemaining--;
        }
        if (TimeWarpTicksRemaining > 0)
        {
            TimeWarpTicksRemaining--;
        }
        if (SlowdownTicksRemaining > 0)
        {
            SlowdownTicksRemaining--;
        }

        PoisonClouds.RemoveAll(cloud =>
        {
            if (cloud.ExpiresAtTick <= TickNumber)
            {
                VacatedLocations.Add(cloud.Location);
                return true;
            }
            return false;
        });

        foreach (var perk in PlayerPerks)
        {
            if (perk.CooldownRemaining > 0)
            {
                perk.CooldownRemaining--;
            }
            if (pressedKey != default && perk.ActivationKey == pressedKey)
            {
                perk.TryActivate(this);
            }
            perk.OnTick(this);
        }
    }

    private void HandleEating()
    {
        var eatenFood = Foods.FirstOrDefault(food => PlayerSnake.Head.Equals(food.Location));
        if (eatenFood == null)
        {
            return;
        }

        var points = eatenFood.Type == FoodType.Gold ? 3 : 1;
        var growth = 1;

        switch (eatenFood.Type)
        {
            case FoodType.Purple:
                PendingPerkChoice = true;
                break;
            case FoodType.Blue:
                ShieldCharges++;
                break;
            case FoodType.Rainbow:
                points += ApplyRainbowEffect();
                break;
        }

        foreach (var perk in PlayerPerks)
        {
            points = perk.ModifyPoints(points, this);
            growth = perk.ModifyGrowth(growth, this);
        }

        for (int i = 0; i < growth; i++)
        {
            PlayerSnake.Grow();
        }
        Foods.Remove(eatenFood);
        FoodsEaten++;
        SoundEvents.Add(eatenFood.Type == FoodType.Red ? SoundEvent.FoodEaten : SoundEvent.LuckyFood);
        EnsureFoodExists();

        LevelPoints += points;
        if (LevelPoints >= Settings.Current.PointsPerLevel)
        {
            LevelPoints -= Settings.Current.PointsPerLevel;
            Level++;
            PendingPerkChoice = true;
            foreach (var perk in PlayerPerks)
            {
                perk.OnLevelUp(this);
            }
        }

        if (PlayerSnake.SnakeBodyParts.Count >= Settings.Current.TargetSnakeLength)
        {
            stopwatch.Stop();
            Status = GameStatus.Won;
            WinnerName = Settings.Current.PlayerName;
            SoundEvents.Add(SoundEvent.Win);
        }
    }

    // A deliberately simpler ruleset for the guest snake in LAN co-op: it grows and scores,
    // but does not touch the host's perk/shield/level state (those are host-only concepts).
    private void HandleGuestEating()
    {
        if (GuestSnake == null)
        {
            return;
        }

        var eatenFood = Foods.FirstOrDefault(food => GuestSnake.Head.Equals(food.Location));
        if (eatenFood == null)
        {
            return;
        }

        GuestSnake.Grow();
        Foods.Remove(eatenFood);
        FoodsEaten++;
        SoundEvents.Add(eatenFood.Type == FoodType.Red ? SoundEvent.FoodEaten : SoundEvent.LuckyFood);
        EnsureFoodExists();

        if (GuestSnake.SnakeBodyParts.Count >= Settings.Current.TargetSnakeLength)
        {
            stopwatch.Stop();
            Status = GameStatus.Won;
            WinnerName = "Guest";
            SoundEvents.Add(SoundEvent.Win);
        }
    }

    // Returns false when the player is dead; perks may intercept the collision.
    private bool TrySurviveCollisions(Location previousTail)
    {
        var isGhost = GhostTicksRemaining > 0;
        var hasWoodpecker = PlayerPerks.Any(perk => perk is WoodpeckerPerk);
        var wasOutOfBounds = PlayerSnake.IsOutOfBounds();
        var hitWall = !isGhost && wasOutOfBounds;
        var hitSelf = !isGhost && PlayerSnake.HasCollidedWithItself();
        var hitEnemy = !isGhost && PlayerSnake.HasHitObstacle(EnemyCells);
        var hitTree = !isGhost && !hasWoodpecker && !wasOutOfBounds && Background.IsCanopyAt(PlayerSnake.Head);
        var hitGuest = !isGhost && GuestSnake != null && GuestAlive
            && GuestSnake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(PlayerSnake.Head));
        if (!hitWall && !hitSelf && !hitEnemy && !hitTree && !hitGuest)
        {
            // A ghost passing through the boundary wraps to the opposite edge instead of
            // vanishing off the visible field (which would also crash the renderer).
            if (wasOutOfBounds)
            {
                WrapPlayerHeadAroundBounds();
            }
            return true;
        }

        if (hitEnemy)
        {
            var spikyTail = PlayerPerks.OfType<SpikyTailPerk>().FirstOrDefault(perk => perk.IsReady);
            if (spikyTail != null)
            {
                var victim = FindEnemyAt(PlayerSnake.Head);
                PlayerSnake.UndoMove(previousTail);
                if (victim != null)
                {
                    KillEnemySnake(victim);
                }
                spikyTail.CooldownRemaining = spikyTail.CooldownTicks;
                SoundEvents.Add(SoundEvent.ShieldAbsorbed);
                return true;
            }
        }

        var ironHead = PlayerPerks.OfType<IronHeadPerk>().FirstOrDefault(perk => perk.ShieldAvailable);
        if (ironHead != null)
        {
            ironHead.ShieldAvailable = false;
            PlayerSnake.UndoMove(previousTail);
            SoundEvents.Add(SoundEvent.ShieldAbsorbed);
            return true;
        }

        // A shield charge from a blue food absorbs the hit.
        if (ShieldCharges > 0)
        {
            ShieldCharges--;
            PlayerSnake.UndoMove(previousTail);
            SoundEvents.Add(SoundEvent.ShieldAbsorbed);
            return true;
        }

        PlayerKilledBy = hitEnemy
            ? FindEnemyAt(PlayerSnake.Head)?.Personality.Name
            : hitGuest ? "Guest" : hitTree ? "A tree" : null;
        stopwatch.Stop();
        Status = GameStatus.GameOver;
        SoundEvents.Add(SoundEvent.PlayerDied);
        return false;
    }

    // The head can only step one cell past an edge in a single tick, so a plain +/- by the map
    // size (rather than a full modulo) is enough to bring it back into the valid, renderable range.
    private void WrapPlayerHeadAroundBounds()
    {
        var head = PlayerSnake.Head;
        if (head.X < 0)
        {
            head.X += Settings.Current.MapWidth;
        }
        else if (head.X >= Settings.Current.MapWidth)
        {
            head.X -= Settings.Current.MapWidth;
        }
        if (head.Y < 0)
        {
            head.Y += Settings.Current.MapHeight;
        }
        else if (head.Y >= Settings.Current.MapHeight)
        {
            head.Y -= Settings.Current.MapHeight;
        }
    }

    // Simpler collision handling for the guest snake in LAN co-op - no perk-based survival.
    // If the host is still alive, the session continues with the guest as a spectator.
    private void TryResolveGuestCollisions()
    {
        if (GuestSnake == null)
        {
            return;
        }

        var hitWall = GuestSnake.IsOutOfBounds();
        var hitSelf = GuestSnake.HasCollidedWithItself();
        var hitEnemy = GuestSnake.HasHitObstacle(EnemyCells);
        var hitTree = !hitWall && Background.IsCanopyAt(GuestSnake.Head);
        var hitHost = PlayerSnake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(GuestSnake.Head));
        if (!hitWall && !hitSelf && !hitEnemy && !hitTree && !hitHost)
        {
            return;
        }

        GuestAlive = false;
        GuestKilledBy = hitEnemy ? FindEnemyAt(GuestSnake.Head)?.Personality.Name
            : hitHost ? Settings.Current.PlayerName : hitTree ? "A tree" : null;
        SoundEvents.Add(SoundEvent.PlayerDied);
        foreach (var bodyPart in GuestSnake.SnakeBodyParts)
        {
            VacatedLocations.Add(bodyPart.Location);
        }
    }

    private ObstacleSnake? FindEnemyAt(Location location)
    {
        return EnemySnakes.FirstOrDefault(enemySnake => enemySnake.Cells.Any(cell => cell.Location.Equals(location)));
    }

    private void MoveEnemySnakes()
    {
        enemyMoveRound++;
        foreach (var enemySnake in EnemySnakes.ToList())
        {
            if (enemyMoveRound % enemySnake.Personality.SpeedDivisor != 0)
            {
                continue;
            }

            // Poisoned or swimming snakes move at half speed.
            if ((IsPoisoned(enemySnake.Cells[0].Location) || Background.IsWaterAt(enemySnake.Cells[0].Location))
                && enemyMoveRound % 2 == 1)
            {
                continue;
            }

            var blockedCells = GetBlockedCells();
            var cellCountBeforeMove = enemySnake.Cells.Count;
            var vacatedTail = enemySnake.Move(this, blockedCells, out var hasDied);

            if (hasDied)
            {
                if (enemySnake.TryPerkSurvive())
                {
                    continue;
                }
                if (enemySnake.Personality.ShrinksInsteadOfDying && enemySnake.Cells.Count >= 6)
                {
                    ShrinkEnemySnake(enemySnake);
                }
                else
                {
                    KillEnemySnake(enemySnake);
                }
                continue;
            }

            if (enemySnake.Cells.Count > cellCountBeforeMove)
            {
                EnemyCells.AddRange(enemySnake.Cells.Skip(cellCountBeforeMove));
                EnsureFoodExists();
            }

            if (vacatedTail != null)
            {
                VacatedLocations.Add(vacatedTail);
            }
        }
    }

    private HashSet<(int X, int Y)> GetBlockedCells()
    {
        var blockedCells = new HashSet<(int X, int Y)>();
        foreach (var cell in EnemyCells)
        {
            blockedCells.Add((cell.Location.X, cell.Location.Y));
        }
        foreach (var bodyPart in PlayerSnake.SnakeBodyParts)
        {
            blockedCells.Add((bodyPart.Location.X, bodyPart.Location.Y));
        }
        if (GuestSnake != null && GuestAlive)
        {
            foreach (var bodyPart in GuestSnake.SnakeBodyParts)
            {
                blockedCells.Add((bodyPart.Location.X, bodyPart.Location.Y));
            }
        }
        // Enemies never enter tree canopies - they have to go around.
        foreach (var canopyCell in Background.CanopyCells)
        {
            blockedCells.Add(canopyCell);
        }
        return blockedCells;
    }

    // A crashed or boxed-in enemy dies and its corpse turns into food, rewarding the hunt.
    // A Spiky Tail owner's corpse is only half edible.
    private void KillEnemySnake(ObstacleSnake enemySnake)
    {
        EnemySnakes.Remove(enemySnake);
        DeadEnemyNames.Add(enemySnake.Personality.Name);
        SoundEvents.Add(SoundEvent.EnemyDied);
        var hasSpikyTail = enemySnake.HasPerk("Spiky Tail");
        for (int i = 0; i < enemySnake.Cells.Count; i++)
        {
            var cell = enemySnake.Cells[i];
            EnemyCells.Remove(cell);
            if (hasSpikyTail && i % 2 == 1)
            {
                VacatedLocations.Add(cell.Location);
            }
            else
            {
                AddCorpseFood(cell.Location);
            }
        }
    }

    // Hydra-style snakes lose their back half instead of dying; the lost half becomes food.
    // With its Metabolism perk earned, Hydra keeps two thirds instead.
    private void ShrinkEnemySnake(ObstacleSnake enemySnake)
    {
        var keptCellCount = enemySnake.HasPerk("Metabolism")
            ? enemySnake.Cells.Count * 2 / 3
            : enemySnake.Cells.Count / 2;
        var lostCells = enemySnake.Cells.Skip(keptCellCount).ToList();
        enemySnake.Cells.RemoveRange(keptCellCount, enemySnake.Cells.Count - keptCellCount);
        foreach (var cell in lostCells)
        {
            EnemyCells.Remove(cell);
            AddCorpseFood(cell.Location);
        }
    }

    private void AddCorpseFood(Location location)
    {
        if (!Foods.Any(food => food.Location.Equals(location)))
        {
            Foods.Add(new FoodInfo(location));
        }
    }

    private void EnsureFoodExists()
    {
        if (Foods.Count == 0)
        {
            FoodInfo food;
            do
            {
                food = FoodProducer.GetFood(PlayerSnake, EnemyCells, Foods, Background);
            } while (GuestSnake != null && GuestAlive
                && GuestSnake.SnakeBodyParts.Any(bodyPart => bodyPart.Location.Equals(food.Location)));
            Foods.Add(food);
        }
    }

    // Rainbow food: one random effect; returns bonus level points (0 or 2).
    private int ApplyRainbowEffect()
    {
        switch (Random.Shared.Next(4))
        {
            case 0:
                GhostTicksRemaining = 15;
                return 0;
            case 1:
                TimeWarpTicksRemaining = 30;
                return 0;
            case 2:
                EmpTicksRemaining = 25;
                return 0;
            default:
                return 2;
        }
    }

    private void ScheduleNextBird()
    {
        if (Settings.Current.BirdIntervalMinutes <= 0)
        {
            nextBirdSpawnTick = -1;
            return;
        }

        // A tick is roughly 100 ms, so one minute is about 600 ticks; +-20% keeps it unpredictable.
        var intervalTicks = Settings.Current.BirdIntervalMinutes * 600;
        nextBirdSpawnTick = TickNumber + intervalTicks * Random.Shared.Next(80, 121) / 100;
    }

    private void UpdateBird()
    {
        if (BirdLocation == null)
        {
            if (nextBirdSpawnTick >= 0 && TickNumber >= nextBirdSpawnTick)
            {
                var fliesRight = Random.Shared.Next(2) == 0;
                birdStepX = fliesRight ? 1 : -1;
                BirdLocation = new Location(
                    fliesRight ? 0 : Settings.Current.MapWidth - 1,
                    Random.Shared.Next(Settings.Current.MapHeight));
            }
            return;
        }

        if (PlayerSnake.Head.Equals(BirdLocation))
        {
            CatchBird();
            return;
        }

        if (TickNumber % 15 == 0)
        {
            SoundEvents.Add(SoundEvent.BirdChirp);
        }

        VacatedLocations.Add(new Location(BirdLocation.X, BirdLocation.Y));
        BirdLocation.X += birdStepX;

        if (BirdLocation.X < 0 || BirdLocation.X >= Settings.Current.MapWidth)
        {
            BirdLocation = null;
            ScheduleNextBird();
            return;
        }

        if (PlayerSnake.Head.Equals(BirdLocation))
        {
            CatchBird();
        }
    }

    // Catching the bird with your head grants an instant perk choice.
    private void CatchBird()
    {
        BirdLocation = null;
        PendingPerkChoice = true;
        SoundEvents.Add(SoundEvent.BirdCaught);
        ScheduleNextBird();
    }

    // Cheat codes typed during gameplay (toggleable in Settings).
    private void CheckCheatCodes(ConsoleKey pressedKey)
    {
        if (!Settings.Current.CheatsEnabled || pressedKey < ConsoleKey.A || pressedKey > ConsoleKey.Z)
        {
            return;
        }

        typedLetters += char.ToLowerInvariant((char)pressedKey);
        if (typedLetters.Length > 12)
        {
            typedLetters = typedLetters[^12..];
        }

        if (typedLetters.EndsWith("god"))
        {
            GhostTicksRemaining = int.MaxValue / 2;
        }
        else if (typedLetters.EndsWith("grow"))
        {
            Level++;
            PendingPerkChoice = true;
        }
        else if (typedLetters.EndsWith("shrink"))
        {
            ShrinkPlayerSnake();
        }
        else if (typedLetters.EndsWith("perk"))
        {
            PendingPerkChoice = true;
        }
        else if (typedLetters.EndsWith("spawnbird"))
        {
            nextBirdSpawnTick = TickNumber;
        }
        else
        {
            return;
        }
        typedLetters = "";
    }

    // Builds a lightweight sparse cell list of everything currently on the field - shared by the
    // death replay (recorded every 2nd tick) and the LAN snapshot sent to the guest (every tick).
    public List<ReplayCell> BuildFrameCells()
    {
        var frame = new List<ReplayCell>();

        foreach (var cloud in PoisonClouds)
        {
            AddReplayCell(frame, cloud.Location, '▒', (110, 220, 50));
        }
        foreach (var enemySnake in EnemySnakes)
        {
            for (int i = 0; i < enemySnake.Cells.Count; i++)
            {
                var (r, g, b) = enemySnake.Personality.BodyColor;
                var fade = 1.0 - 0.4 * i / Math.Max(1, enemySnake.Cells.Count - 1);
                AddReplayCell(frame, enemySnake.Cells[i].Location, '█', ((int)(r * fade), (int)(g * fade), (int)(b * fade)));
            }
        }
        for (int i = 0; i < PlayerSnake.SnakeBodyParts.Count; i++)
        {
            var bodyPart = PlayerSnake.SnakeBodyParts[i];
            AddReplayCell(frame, bodyPart.Location, bodyPart.DisplayChar, RainbowColor.Get(i * 12));
        }
        if (GuestSnake != null && GuestAlive)
        {
            for (int i = 0; i < GuestSnake.SnakeBodyParts.Count; i++)
            {
                var bodyPart = GuestSnake.SnakeBodyParts[i];
                // Cyan-phased so the guest is visually distinct from the host's rainbow snake.
                AddReplayCell(frame, bodyPart.Location, bodyPart.DisplayChar, RainbowColor.Get(180 + i * 12));
            }
        }
        foreach (var food in Foods)
        {
            AddReplayCell(frame, food.Location, '★', GetReplayFoodColor(food.Type));
        }
        if (BirdLocation != null)
        {
            AddReplayCell(frame, BirdLocation, 'V', (255, 240, 80));
        }

        return frame;
    }

    private void RecordReplayFrame()
    {
        ReplayFrames.Enqueue(BuildFrameCells());
        while (ReplayFrames.Count > 50)
        {
            ReplayFrames.Dequeue();
        }
    }

    private static void AddReplayCell(List<ReplayCell> frame, Location location, char displayChar, (int R, int G, int B) color)
    {
        if (location.X >= 0 && location.X < Settings.Current.MapWidth
            && location.Y >= 0 && location.Y < Settings.Current.MapHeight)
        {
            frame.Add(new ReplayCell(location.X, location.Y, displayChar, color));
        }
    }

    private static (int R, int G, int B) GetReplayFoodColor(FoodType type)
    {
        return type switch
        {
            FoodType.Gold => (255, 200, 40),
            FoodType.Purple => (190, 60, 255),
            FoodType.Blue => (70, 130, 255),
            FoodType.Rainbow => (255, 255, 255),
            _ => (255, 60, 40)
        };
    }

    // The shrink cheat: back to 3 parts, level points kept.
    private void ShrinkPlayerSnake()
    {
        var bodyParts = PlayerSnake.SnakeBodyParts;
        while (bodyParts.Count > 3)
        {
            VacatedLocations.Add(bodyParts[^1].Location);
            bodyParts.RemoveAt(bodyParts.Count - 1);
        }
    }
}
