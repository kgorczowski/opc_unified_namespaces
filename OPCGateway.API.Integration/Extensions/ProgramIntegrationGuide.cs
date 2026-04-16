// ────────────────────────────────────────────────────────────────────────────
// HOW TO INTEGRATE INTO OPCGateway/Program.cs
// ────────────────────────────────────────────────────────────────────────────
// This file is NOT a standalone Program.cs.
// It shows the exact additions required to the existing OPCGateway API Program.cs.
//
// STEP 1 – Add package references to OPCGateway/OPCGateway.csproj:
//
//   <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
//   <PackageReference Include="protobuf-net.Grpc.AspNetCore" Version="1.1.1" />
//   <PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />
//   <PackageReference Include="Npgsql" Version="8.0.3" />
//
// STEP 2 – Add project references:
//
//   <ProjectReference Include="..\OPCGateway.Admin.Contracts\OPCGateway.Admin.Contracts.csproj" />
//   <ProjectReference Include="..\OPCGateway.Admin.Server\OPCGateway.Admin.Server.csproj" />
//   <ProjectReference Include="..\OPCGateway.Worker\OPCGateway.Worker.csproj" />
//
// STEP 3 – In Program.cs, add the following lines (marked with // [NEW]):

// using OPCGateway.Infrastructure.MessageBus;          // [NEW]
// using OPCGateway.Admin.Server.Extensions;             // [NEW]

var builder = WebApplication.CreateBuilder(args);

// ... existing registrations ...

// [NEW] Message bus (Valkey)
builder.Services.AddMessageBus(builder.Configuration);

// [NEW] Admin gRPC services
builder.Services.AddAdminGrpcServices();

// [NEW] HTTP/2 required for gRPC – enable Kestrel on a dedicated port
builder.WebHost.ConfigureKestrel(kestrel =>
{
    var grpcPort = builder.Configuration.GetValue<int>("Grpc:Port", 5002);
    kestrel.ListenAnyIP(grpcPort, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// ─── after app = builder.Build() ─────────────────────────────────────────

// [NEW] Map admin gRPC endpoints
app.MapAdminGrpcServices();

// [NEW] Minimal health endpoint for Docker healthcheck
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// ... existing middleware and app.Run() ...
