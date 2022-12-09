using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;
using SnakeGameEngine.Moving;

namespace SnakeGameEngine
{
    public class GameEngine
    {
        public void NewGame() 
        {
            var foodInfo = GetFood();
            ConsoleKeyInfo consoleKeyInfo;
            var snake = new Snake();

            do
            {
                consoleKeyInfo = snake.SetDirection();

                if (snake.HasTouchedFood(foodInfo))
                {
                    foodInfo = GetFood();
                }

                snake.Move();
                ConsoleDrawer.DrawScreen(foodInfo, snake);
                Thread.Sleep(140);
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }

        private static FoodInfo GetFood()
        {
            return new FoodInfo(new Location(Random.Shared.Next(Constants.MaxX), Random.Shared.Next(Constants.MaxY)));
        }
    }
}
