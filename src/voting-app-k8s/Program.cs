using Orleans.Configuration;
using Orleans.Dashboard;
using Voting.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleans((ctx, siloBuilder) =>
{
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        // During development time, we don't want to have to deal with
        // storage emulators or other dependencies. Just "Hit F5" to run.
        siloBuilder
            .UseLocalhostClustering()
            .AddMemoryGrainStorage("votes");
    }
    else
    {
        // In Kubernetes, the default UseKubernetesHosting() uses environment variables and the pod manifest to populate ClusterId and ServiceId from the following environment variables: ORLEANS_SERVICE_ID & ORLEANS_CLUSTER_ID, which can be bound to the pod labels.
        // It can also be customize as needed, for example, to use a specific Cluster and Service IDs:
        //siloBuilder.Configure<ClusterOptions>(options =>
        //{
        //    options.ClusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") ?? "votingapp-cluster";
        //    options.ServiceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") ?? "votingapp-service";
        //});

        // This enables Kubernetes membership & networking integration
        siloBuilder.UseKubernetesHosting();

        // Use Redis for clustering & persistence
        var redisAddress = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
        siloBuilder.UseRedisClustering(options => options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisAddress));
        siloBuilder.AddRedisGrainStorage("votes", options => options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisAddress));
    }

    // Add the dashboard
    siloBuilder
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
