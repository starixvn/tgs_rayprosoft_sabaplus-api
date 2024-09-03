#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["SabaPlus.API/SabaPlus.API.csproj", "SabaPlus.API/"]
COPY ["SabaPlus.Repositories/SabaPlus.Repositories.csproj", "SabaPlus.Repositories/"]
COPY ["SabaPlus.Common/SabaPlus.Common.csproj", "SabaPlus.Common/"]
COPY ["SabaPlus.Core/SabaPlus.Core.csproj", "SabaPlus.Core/"]
RUN dotnet restore "./SabaPlus.API/./SabaPlus.API.csproj"
COPY . .
WORKDIR "/src/SabaPlus.API"
RUN dotnet build "./SabaPlus.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SabaPlus.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SabaPlus.API.dll"]