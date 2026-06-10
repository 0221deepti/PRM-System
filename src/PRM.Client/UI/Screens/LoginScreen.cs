using PRM.Client.UI;

namespace PRM.Client.UI.Screens;

public class LoginScreen : Screen
{
    public LoginScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Login to PRM");

        var username = InputHelper.ReadString("Username");
        var password = InputHelper.ReadPassword("Password");

        try
        {
            await _services.Auth.LoginAsync(username, password);
            ConsoleRenderer.RenderSuccess($"Welcome back, {_services.Session.UserFullName}!");
            ConsoleRenderer.Pause();

            // Password reset check is usually handled by a custom claim or DB flag,
            // but for simplicity, we could just check if password is 'admin123'
            if (password == "admin123" && _services.Session.Role == Domain.Enums.UserRole.Admin)
            {
                ConsoleRenderer.RenderWarning("You are using the default admin password. Please change it.");
                await new ChangePasswordScreen(_services).RenderAsync();
            }

            return false; // Exit login loop to go to main menu
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
            return true; // Stay on login screen
        }
    }
}

public class ChangePasswordScreen : Screen
{
    public ChangePasswordScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Change Password");

        var newPass = InputHelper.ReadPassword("New Password");
        var confirmPass = InputHelper.ReadPassword("Confirm Password");

        if (newPass != confirmPass)
        {
            ConsoleRenderer.RenderError("Passwords do not match.");
            ConsoleRenderer.Pause();
            return true;
        }

        try
        {
            await _services.Auth.ChangePasswordAsync(newPass, confirmPass);
            ConsoleRenderer.RenderSuccess("Password changed successfully.");
            ConsoleRenderer.Pause();
            return false;
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
            return true;
        }
    }
}
