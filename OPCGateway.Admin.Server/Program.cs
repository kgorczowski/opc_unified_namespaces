// Copyright (c) 2025 vm.pl

using OPCGateway.Admin.Server.Extensions;
using OPCGateway.Admin.Server.Services;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCodeFirstGrpc();
builder.Services.AddAdminRepositories(builder.Configuration);

var app = builder.Build();

app.MapGrpcService<ServerManagementService>();
app.MapGrpcService<NodeManagementService>();
app.MapGrpcService<NamespaceManagementService>();

app.Run();
