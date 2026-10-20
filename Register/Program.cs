using Microsoft.AspNetCore.Builder;
using Register;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigurationServices(builder.Configuration);
        var app = builder.Build();
        var dapper = new RegisterDBDapper("connection");
        app.MapApplication();
        app.Run();
        //Demo.Run();
    }
}