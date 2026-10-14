FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Judhur.Api/Judhur.Api.csproj", "src/Judhur.Api/"]
COPY ["src/Judhur.Application/Judhur.Application.csproj", "src/Judhur.Application/"]
COPY ["src/Judhur.Infrastructure/Judhur.Infrastructure.csproj", "src/Judhur.Infrastructure/"]
COPY ["src/Judhur.Domain/Judhur.Domain.csproj", "src/Judhur.Domain/"]
COPY ["src/Judhur.Contracts/Judhur.Contracts.csproj", "src/Judhur.Contracts/"]
RUN dotnet restore "./src/Judhur.Api/Judhur.Api.csproj"
COPY . .
WORKDIR "/src/src/Judhur.Api"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Judhur.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Judhur.Api.dll"]
