using Library.API.Endpoints.Internal;
using Microsoft.AspNetCore.Cors;

namespace Library.API.Endpoints;

public class StatusEndpoints : IEndpoints
{
    public static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        
    }

    public static void DefineEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("status", [EnableCors("policyName")] () =>
        {
            return Results.Extensions.Html(@"<div>page status</div>");
        }).ExcludeFromDescription();//.RequireCors(_policyName);
    }
}
