using PRM.Client.UI;
using PRM.Application.DTOs.User;
using PRM.Domain.Enums;

namespace PRM.Client.UI.Screens;

public class ManageUsersScreen : Screen
{
    public ManageUsersScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Manage Users");

        var users = await _services.Users.GetAllUsersAsync();

        Console.WriteLine("Users:");
        foreach (var u in users)
        {
            Console.WriteLine($"{u.Id}. {u.FullName} ({u.Username}) - {u.RoleName} - {(u.IsActive ? "Active" : "Inactive")}");
        }

        Console.WriteLine();
        Console.WriteLine("1. Create User");
        Console.WriteLine("2. Deactivate User");
        Console.WriteLine("3. Reactivate User");
        Console.WriteLine("0. Back");

        var choice = InputHelper.ReadString("Select an option");
        try
        {
            switch (choice)
            {
                case "1":
                    await CreateUserAsync();
                    break;
                case "2":
                    {
                        var id = InputHelper.ReadInt("User Id to deactivate");
                        await _services.Users.DeactivateUserAsync(id);
                        ConsoleRenderer.RenderSuccess("User deactivated.");
                        ConsoleRenderer.Pause();
                    }
                    break;
                case "3":
                    {
                        var id = InputHelper.ReadInt("User Id to reactivate");
                        await _services.Users.ReactivateUserAsync(id);
                        ConsoleRenderer.RenderSuccess("User reactivated.");
                        ConsoleRenderer.Pause();
                    }
                    break;
                case "0":
                    return false;
                default:
                    ConsoleRenderer.RenderError("Invalid option.");
                    ConsoleRenderer.Pause();
                    break;
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
        }

        return true;
    }

    private async Task CreateUserAsync()
    {
        ShowHeader("Create User");
        var fullName = InputHelper.ReadString("Full Name");
        var email = InputHelper.ReadString("Email");
        var username = InputHelper.ReadString("Username");
        var tempPass = InputHelper.ReadPassword("Temporary Password");

        Console.WriteLine("Select role:");
        Console.WriteLine("1. Admin");
        Console.WriteLine("2. Manager");
        Console.WriteLine("3. Employee");
        var roleChoice = InputHelper.ReadString("Role (1-3)");
        var roleId = roleChoice switch
        {
            "1" => 1,
            "2" => 2,
            _ => 3
        };

        var dto = new CreateUserDto(fullName, email, username, tempPass, roleId);
        await _services.Users.CreateUserAsync(dto);
        ConsoleRenderer.RenderSuccess("User created.");
        ConsoleRenderer.Pause();
    }
}
