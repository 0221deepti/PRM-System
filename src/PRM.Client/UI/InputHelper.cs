using System;
using System.Globalization;
using System.Linq;

namespace PRM.Client.UI;

/// <summary>
/// Helper for reading and validating console input.
/// </summary>
public static class InputHelper
{
    public static string ReadString(string prompt, bool required = true)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input)) return input;
            if (!required) return string.Empty;
            ConsoleRenderer.RenderError("This field is required.");
        }
    }

    public static string ReadPassword(string prompt)
    {
        Console.Write($"{prompt}: ");
        var pass = string.Empty;
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && pass.Length > 0)
            {
                Console.Write("\b \b");
                pass = pass[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                pass += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);
        
        Console.WriteLine();
        return pass;
    }

    public static int ReadInt(string prompt, bool required = true)
    {
        bool isId = prompt.Contains("Id", StringComparison.OrdinalIgnoreCase);
        while (true)
        {
            var str = ReadString(prompt, required);
            if (!required && string.IsNullOrEmpty(str)) return 0;
            if (int.TryParse(str, out var num))
            {
                if (isId && num <= 0)
                {
                    ConsoleRenderer.RenderError("Please enter a valid positive ID.");
                    continue;
                }
                return num;
            }
            ConsoleRenderer.RenderError("Please enter a valid integer.");
        }
    }

    public static decimal ReadDecimal(string prompt, bool required = true)
    {
        while (true)
        {
            var str = ReadString(prompt, required);
            if (!required && string.IsNullOrEmpty(str)) return 0;
            if (decimal.TryParse(str, out var num)) return num;
            ConsoleRenderer.RenderError("Please enter a valid number.");
        }
    }

    public static DateOnly ReadDate(string prompt, bool required = true)
    {
        while (true)
        {
            var str = ReadString($"{prompt} (DD-MM-YYYY)", required);
            if (!required && string.IsNullOrEmpty(str)) return DateOnly.MinValue;
            if (DateOnly.TryParseExact(str, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            ConsoleRenderer.RenderError("Please enter a valid calendar date in DD-MM-YYYY format.");
        }
    }

    public static bool ReadBool(string prompt)
    {
        while (true)
        {
            var str = ReadString($"{prompt} (y/n)");
            if (str.Equals("y", StringComparison.OrdinalIgnoreCase)) return true;
            if (str.Equals("n", StringComparison.OrdinalIgnoreCase)) return false;
            ConsoleRenderer.RenderError("Please enter 'y' for Yes or 'n' for No.");
        }
    }

    public static int ReadPercentage(string prompt, bool required = true)
    {
        while (true)
        {
            var str = ReadString(prompt, required);
            if (!required && string.IsNullOrEmpty(str)) return 0;

            var cleaned = str.Trim();
            if (cleaned.EndsWith("%"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 1).Trim();
            }

            if (cleaned.Contains("%") || !int.TryParse(cleaned, out var percent))
            {
                ConsoleRenderer.RenderError("Please enter a valid percentage (e.g. 30 or 30%).");
                continue;
            }

            if (percent < 1 || percent > 100)
            {
                ConsoleRenderer.RenderError("Utilisation percent must be between 1 and 100.");
                continue;
            }

            return percent;
        }
    }

    public static string ReadMenuOption(string prompt, string[] validOptions)
    {
        while (true)
        {
            var choice = ReadString(prompt).Trim();
            if (validOptions.Contains(choice)) return choice;
            ConsoleRenderer.RenderError("Invalid option. Please select a valid menu option.");
        }
    }

    public static T ReadEnumSelection<T>(string prompt, int maxOption) where T : struct, Enum
    {
        while (true)
        {
            var str = ReadString($"{prompt} (1-{maxOption})");
            if (int.TryParse(str, out var val) && val >= 1 && val <= maxOption)
            {
                return (T)(object)(val - 1);
            }
            ConsoleRenderer.RenderError($"Please select a valid option (1-{maxOption}).");
        }
    }
}
