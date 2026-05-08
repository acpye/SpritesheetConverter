using _3DSpritesheetConverter.Scenes;

public static class Program
{
    public static void Main()
    {
        using(Game game = new Game())
        {
            game.Run();
        }
    }
}