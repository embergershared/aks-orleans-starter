using Orleans.Dashboard;
using Voting.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleans((ctx, orleansBuilder) =>
{
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        // During development time, we don't want to have to deal with
        // storage emulators or other dependencies. Just "Hit F5" to run.
        orleansBuilder
            .UseLocalhostClustering()
            .AddMemoryGrainStorage("votes");
    }
    else
    {
        // In Kubernetes, we use environment variables and the pod manifest
        orleansBuilder.UseKubernetesHosting();

        // Use Redis for clustering & persistence
        var redisAddress = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
        orleansBuilder.UseRedisClustering(options => options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisAddress));
        orleansBuilder.AddRedisGrainStorage("votes", options => options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisAddress));
    }

    // Add the dashboard
    orleansBuilder
        .AddDashboard();
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<PollService>();
builder.Services.AddScoped<DemoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapOrleansDashboard(routePrefix: "/dashboard");
app.Run();
