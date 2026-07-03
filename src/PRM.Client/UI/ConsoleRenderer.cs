namespace PRM.Client.UI;

/// <summary>
/// Helper class for rendering UI elements like headers, errors, and tables in the console.
/// </summary>
public static class ConsoleRenderer
{
    public static void Clear() => Console.Clear();

    public static void RenderHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('=', 50));
        Console.WriteLine($"   {title.ToUpper()}");
        Console.WriteLine(new string('=', 50));
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void RenderError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ERROR] {message}\n");
        Console.ResetColor();
    }

    public static void RenderSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[SUCCESS] {message}\n");
        Console.ResetColor();
    }

    public static void RenderWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[WARNING] {message}\n");
        Console.ResetColor();
    }

    public static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }
}
