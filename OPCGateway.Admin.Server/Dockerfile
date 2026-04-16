FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OPCGateway.Admin.Server/OPCGateway.Admin.Server.csproj", "OPCGateway.Admin.Server/"]
COPY ["OPCGateway.Admin.Contracts/OPCGateway.Admin.Contracts.csproj", "OPCGateway.Admin.Contracts/"]
RUN dotnet restore "OPCGateway.Admin.Server/OPCGateway.Admin.Server.csproj"

COPY . .
WORKDIR "/src/OPCGateway.Admin.Server"
RUN dotnet build "OPCGateway.Admin.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OPCGateway.Admin.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OPCGateway.Admin.Server.dll"]
