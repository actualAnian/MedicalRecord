using Microsoft.AspNetCore.Builder;
using Register.App;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigurationServices(builder.Configuration);
        var app = builder.Build();
        app.MapApplication();
        app.Run();
        //Demo.Run();
    }
}