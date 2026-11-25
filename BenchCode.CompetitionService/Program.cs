using OpenIddict.Validation.AspNetCore;

namespace BenchCode.CompetitionService;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddNpgsqlDbContext<CompetitionDbContext>(connectionName: "competitionsdb");
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        builder.Services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.UseSystemNetHttp();
                var issuer = builder.Configuration["OpenIddict:Issuer"];
                if (!string.IsNullOrWhiteSpace(issuer))
                    options.SetIssuer(issuer);
                options.AddAudiences("benchcode-api");
                options.UseAspNetCore();
            });

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}