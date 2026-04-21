# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Create certificate directories and set permissions
RUN mkdir -p /app/certificates/server1/MachineDefault \
           /app/certificates/server1/UA\ Certificate\ Authorities \
           /app/certificates/server1/UA\ Applications \
           /app/certificates/server1/RejectedCertificates \
           /app/certificates/server2/MachineDefault \
           /app/certificates/server2/UA\ Certificate\ Authorities \
           /app/certificates/server2/UA\ Applications \
           /app/certificates/server2/RejectedCertificates && \
    chown -R app:app /app/certificates && \
    chmod -R 755 /app/certificates

EXPOSE 4841
EXPOSE 4842

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OPCGateway.OPCServerMock.csproj", "./"]
RUN dotnet restore "./OPCGateway.OPCServerMock.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "./OPCGateway.OPCServerMock.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OPCGateway.OPCServerMock.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "OPCGateway.OPCServerMock.dll"]