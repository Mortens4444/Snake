namespace SnakeGameEngine;

public interface IRenderer
{
    void BeginGame(GameState gameState);

    void DrawFrame(GameState gameState);

    void ShowPaused();

    void PlayReplay(GameState gameState);
}
