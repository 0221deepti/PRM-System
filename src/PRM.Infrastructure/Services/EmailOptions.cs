namespace PRM.Infrastructure.Services;

public class EmailOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@prm.local";
    public string FromName { get; set; } = "PRM System";
    public bool EnableSsl { get; set; } = true;
}