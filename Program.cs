using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

using MetadataPopulator = System.Func<System.Reflection.MethodInfo, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions?, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult>;
using RequestDelegateFactoryFunc = System.Func<System.Delegate, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult?, Microsoft.AspNetCore.Http.RequestDelegateResult>;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

MapEndpoints<Todo>(app);
MapDiffEndpoints<Todo, Project>(app);
MapDiffEndpoints2<Project>(app);
app.MapPost("/4", (Todo todo) => todo.IsCompleted);

app.Run();

static void MapEndpoints<T>(IEndpointRouteBuilder app)
{
    app.MapPost("/1/{name}", (string name, T input) => $"Hello {name}! {input.GetType()}");
}

static void MapDiffEndpoints<T, U>(IEndpointRouteBuilder app)
{
    app.MapPost("/2/{name}", (string name, T input, U anotherOne) => $"Hello {name}! {input.GetType()}");
}

static void MapDiffEndpoints2<T>(IEndpointRouteBuilder app)
{
    app.MapPost("/3/", (T input) => $"Hello world! {input.GetType()}");
}

public static class Maps
{
    public static readonly Dictionary<string, (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
    {
        ["baz"] = (
            (methodInfo, options) =>
            {
                return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
            },
            (del, options, inferredMetadataResult) =>
            {
                var handler = (Func<Todo, bool>)del;
                return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated from baz: " + handler(default(Todo))), ReadOnlyCollection<object>.Empty);
            }
        ),
    };
}

public static class Maps<T>
{
    public static readonly Dictionary<string, (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
    {
        ["foo"] = (
            (methodInfo, options) =>
            {
                return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
            },
            (del, options, inferredMetadataResult) =>
            {
                var handler = (Func<string, T, string>)del;
                return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated from foo: " + handler("foo", default(T))), ReadOnlyCollection<object>.Empty);
            }
        ),
        ["bar"] = (
            (methodInfo, options) =>
            {
                return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
            },
            (del, options, inferredMetadataResult) =>
            {
                var handler = (Func<T, string>)del;
                return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated from bar: " + handler(default(T))), ReadOnlyCollection<object>.Empty);
            }
        ),
    };
}

public static class Maps<T, U>
{
    public static readonly Dictionary<string, (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
    {
        ["foo"] = (
            (methodInfo, options) =>
            {
                return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
            },
            (del, options, inferredMetadataResult) =>
            {
                var handler = (Func<string, T, U, string>)del;
                return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated:" + handler("bar", default(T), default(U))), ReadOnlyCollection<object>.Empty);
            }
        ),
    };
}

public static class Extensions
{
    public static RouteHandlerBuilder MapPost<T>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<string, T, string> handler)
    {
        System.Console.WriteLine("overriden");
        var (populateMetadata, createRequestDelegate) = Maps<T>.map["foo"];
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }

    public static RouteHandlerBuilder MapPost<T>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<T, string> handler)
    {
        var (populateMetadata, createRequestDelegate) = Maps<T>.map["bar"];
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }

    public static RouteHandlerBuilder MapPost<T, U>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<string, T, U, string> handler)
    {
        System.Console.WriteLine("Override");
        var (populateMetadata, createRequestDelegate) = Maps<T, U>.map["foo"];
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }

    public static RouteHandlerBuilder MapPost(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<Todo, bool> handler)
    {
        System.Console.WriteLine("MapPostwithoutgeneric");
        var (populateMetadata, createRequestDelegate) = Maps.map["baz"];
        return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
    }
}

// public static class Extensions
// {
//     public static RouteHandlerBuilder MapPost<T>(
//         this IEndpointRouteBuilder endpoints,
//         [StringSyntax("Route")] string pattern,
//         Func<string, T, string> handler) // Or System.Delegate with interceptors
//     {
//         MetadataPopulator populateMetadata = (methodInfo, options) =>
//         {
//             return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
//         };
//         RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>
//         {
//             var handler = (Func<string, T, string>)del;
//             return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated: " + handler("foo", default(T))), ReadOnlyCollection<object>.Empty);
//         };
//         return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
//     }

//     public static RouteHandlerBuilder MapPost<T, U>(
//         this IEndpointRouteBuilder endpoints,
//         [StringSyntax("Route")] string pattern,
//         Func<string, T, U, string> handler) // Or System.Delegate with interceptors
//     {
//         MetadataPopulator populateMetadata = (methodInfo, options) =>
//         {
//             return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
//         };
//         RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>
//         {
//             var handler = (Func<string, T, U, string>)del;
//             return new RequestDelegateResult((HttpContext httpContext) => httpContext.Response.WriteAsync("Generated: " + handler("bar", default(T), default(U))), ReadOnlyCollection<object>.Empty);
//         };
//         return RouteHandlerServices.Map(endpoints, pattern, handler, new[] { "POST" }, populateMetadata, createRequestDelegate);
//     }
// }

public record struct Todo(string Task, bool IsCompleted);
public record struct Project(string Name, string Owner);