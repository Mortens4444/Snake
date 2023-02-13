using SnakeGameEngine.Actors;
using SnakeGameEngine.ConsoleUtils;

namespace SnakeGameEngine
{
    public static class GameEngine
    {
        public static void NewGame() 
        {
            var foodInfo = FoodProducer.GetFood();
            ConsoleKeyInfo consoleKeyInfo;
            var snake = new Snake();

            do
            {
                consoleKeyInfo = snake.SetDirection();

                if (snake.HasTouchedFood(foodInfo))
                {
                    ConsoleDrawer.ClearLocation(foodInfo.Location);
                    foodInfo = FoodProducer.GetFood();
                }

                ConsoleDrawer.ClearLocation(snake.Tail);
                snake.Move();
                ConsoleDrawer.DrawScreen(foodInfo, snake);
                Thread.Sleep(100);
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        }
    }
}
