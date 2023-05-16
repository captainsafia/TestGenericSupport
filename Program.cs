using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

using MetadataPopulator = System.Func<System.Reflection.MethodInfo, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions?, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult>;
using RequestDelegateFactoryFunc = System.Func<System.Delegate, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult?, Microsoft.AspNetCore.Http.RequestDelegateResult>;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

MapEndpoints<Todo>(app);
MapDiffEndpoints<Todo, Project>(app);
MapClass<Todo>.MapDiffEndpoints2<Project>(app);

app.Run();

static void MapEndpoints<T>(IEndpointRouteBuilder app)
{
    app.MapPost("/1/{name}", (string name, T input) => $"Hello {name}! {input.GetType()}");
}

static void MapDiffEndpoints<T, U>(IEndpointRouteBuilder app)
{
    app.MapPost("/2/{name}", (string name, T input, U anotherOne) => $"Hello {name}! {input.GetType()}");
}

public static class MapClass<V>
{
    public static void MapDiffEndpoints2<T>(IEndpointRouteBuilder app)
    {
        app.MapPost("/3/", (int id, T input) => $"Hello world! {input.GetType()}");
    }
}


public static class Extensions
{
    // Intercepts handler with route "/1/name"
    // T0 = T
    public static RouteHandlerBuilder MapPost<T0>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<string, T0, string> handler) // Or System.Delegate with interceptors
    {
        MetadataPopulator populateMetadata = (methodInfo, options) =>
        {
            return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
        };
        RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>
        {
            var handler = (Func<string, T0, string>)del;
            return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated: " + handler("foo", default(T0))), ReadOnlyCollection<object>.Empty);
        };
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }

    // Intercepts handler with route "/2/name"
    // T0 = T
    // T1 = U
    public static RouteHandlerBuilder MapPost_1<T0, T1>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<string, T0, T1, string> handler) // Or System.Delegate with interceptors
    {
        MetadataPopulator populateMetadata = (methodInfo, options) =>
        {
            return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
        };
        RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>
        {
            var handler = (Func<string, T0, T1, string>)del;
            return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated: " + handler("bar", default(T0), default(T1))), ReadOnlyCollection<object>.Empty);
        };
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }

    // Intercepts handler with route "/3"/
    // T0 = V
    // T1 = T
    public static RouteHandlerBuilder MapPost_2<T0, T1>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<string, T0, T1, string> handler) // Or System.Delegate with interceptors
    {
        MetadataPopulator populateMetadata = (methodInfo, options) =>
        {
            return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
        };
        RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>
        {
            var handler = (Func<int, T0, T1, string>)del;
            return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated: " + handler(1, default(T0), default(T1))), ReadOnlyCollection<object>.Empty);
        };
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }
}

public record struct Todo(string Task, bool IsCompleted);
public record struct Project(string Name, string Owner);