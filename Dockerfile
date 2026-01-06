# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MortysDBot.sln ./
COPY src ./src

RUN dotnet restore ./src/MortysDBot.Bot/MortysDBot.Bot.csproj
RUN dotnet publish ./src/MortysDBot.Bot/MortysDBot.Bot.csproj -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && \
    useradd -m botuser && \
    chown -R botuser:botuser /app

USER botuser

ENTRYPOINT ["dotnet", "MortysDBot.Bot.dll"]
