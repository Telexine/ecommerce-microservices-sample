using BuildingBlocks.Logging;
using BuildingBlocks.Security;
using BuildingBlocks.Swagger;
using BuildingBlocks.Web;
using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.Extensions.ServiceCollectionExtensions;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Serilog;
using ECommerce.Services.Identity;
using ECommerce.Services.Identity.Api.Extensions.ApplicationBuilderExtensions;
using ECommerce.Services.Identity.Api.Extensions.ServiceCollectionExtensions;

// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
// https://benfoster.io/blog/mvc-to-minimal-apis-aspnet-6/
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((env, c) =>
{
    // Handling Captive Dependency Problem
    // https://ankitvijay.net/2020/03/17/net-core-and-di-beware-of-captive-dependency/
    // https://levelup.gitconnected.com/top-misconceptions-about-dependency-injection-in-asp-net-core-c6a7afd14eb4
    // https://blog.ploeh.dk/2014/06/02/captive-dependency/
    if (env.HostingEnvironment.IsDevelopment() || env.HostingEnvironment.IsEnvironment("tests") ||
        env.HostingEnvironment.IsStaging())
    {
        c.ValidateScopes = true;
    }
});

builder.Services.AddControllers(options =>
        options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer())))
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.AddCompression();

builder.AddCustomProblemDetails();

builder.Host.AddCustomSerilog();

builder.AddCustomSwagger(builder.Configuration, typeof(IdentityRoot).Assembly);

builder.Services.AddHttpContextAccessor();

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddCustomAuthorization();

/*----------------- Module Services Setup ------------------*/
builder.AddIdentityModule();

var app = builder.Build();

var environment = app.Environment;

if (environment.IsDevelopment() || environment.IsEnvironment("docker"))
{
    app.UseDeveloperExceptionPage();

    // Minimal Api not supported versioning in .net 6
    app.UseCustomSwagger();

    // ref: https://christian-schou.dk/how-to-make-api-documentation-using-swagger/
    app.UseReDoc(options =>
    {
        options.DocumentTitle = "Identity Service ReDoc";
        options.SpecUrl = "/swagger/v1/swagger.json";
    });
}

app.UseProblemDetails();

app.UseRouting();

app.UseAppCors();

/*----------------- Module Middleware Setup ------------------*/
await app.ConfigureIdentityModule(environment, app.Logger);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/*----------------- Module Routes Setup ------------------*/
app.MapIdentityModuleEndpoints();

// automatic discover minimal endpoints
app.MapEndpoints();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

await app.RunAsync();


namespace ECommerce.Services.Identity.Api
{
    public partial class Program
    {
    }
}
