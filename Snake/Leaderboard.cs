namespace SnakeGameEngine
{
    public class Leaderboard
    {
        private readonly string filePath;

        public Leaderboard(string filePath)
        {
            this.filePath = filePath;
        }

        public void AddScore(string playerName, int score)
        {
            File.AppendAllLines(filePath, new[] { $"{playerName},{score}" });
        }

        public List<KeyValuePair<string, int>> GetTopScores(int count)
        {
            if (!File.Exists(filePath))
            {
                return new List<KeyValuePair<string, int>>();
            }

            var scores = File.ReadAllLines(filePath)
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new KeyValuePair<string, int>(parts[0], int.Parse(parts[1]));
                })
                .OrderByDescending(entry => entry.Value)
                .Take(count)
                .ToList();

            return scores;
        }
    }
}
