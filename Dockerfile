# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY StreamsJoiner/StreamsJoiner.csproj StreamsJoiner/
COPY StreamsJoiner.Core/StreamsJoiner.Core.csproj StreamsJoiner.Core/
COPY StreamsJoiner.Messaging/StreamsJoiner.Messaging.csproj StreamsJoiner.Messaging/
RUN dotnet restore StreamsJoiner/StreamsJoiner.csproj
COPY . .
RUN dotnet publish StreamsJoiner/StreamsJoiner.csproj -c Release -o /app

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "StreamsJoiner.dll"]
