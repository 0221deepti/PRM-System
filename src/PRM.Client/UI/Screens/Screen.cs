namespace PRM.Client.UI.Screens;

public abstract class Screen
{
    protected readonly AppServices _services;

    protected Screen(AppServices services)
    {
        _services = services;
    }

    /// <summary>
    /// Executes the screen's main loop. Returns true to stay on this screen,
    /// false to go back/exit.
    /// </summary>
    public abstract Task<bool> RenderAsync();

    protected void ShowHeader(string title)
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.RenderHeader(title);
        
        if (_services.Session.IsLoggedIn)
        {
            Console.WriteLine($"Logged in as: {_services.Session.UserFullName} [{_services.Session.Role}]");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine();
        }
    }
}
