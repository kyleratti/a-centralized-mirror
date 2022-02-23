FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BackgroundProcessor/BackgroundProcessor.csproj", "BackgroundProcessor/"]
RUN dotnet restore "BackgroundProcessor/BackgroundProcessor.csproj"
COPY . .
WORKDIR "/src/BackgroundProcessor"
RUN dotnet build "BackgroundProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackgroundProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackgroundProcessor.dll"]
