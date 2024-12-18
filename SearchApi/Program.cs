using SearchApi.Services;

namespace SearchApi;

public static class Program
{
    private const string CorsPolicyName = "AllowLocalhost3000";
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddGrpc();
        builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, p =>
        {
            p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        }));
        
        var app = builder.Build();
        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
        app.UseCors();
        
        app.MapGrpcService<SearchService>()
            .EnableGrpcWeb()
            .RequireCors(CorsPolicyName);
        app.MapGet("/", () => "This gRPC service is gRPC-Web enabled, CORS enabled, and is callable from browser apps using the gRPC-Web protocol");
        app.Run();
    }
}