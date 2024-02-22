FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CleanFtp/CleanFtp.csproj", "CleanFtp/"]
RUN dotnet restore "CleanFtp/CleanFtp.csproj"
COPY . .
WORKDIR "/src/CleanFtp"
RUN dotnet build "CleanFtp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CleanFtp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleanFtp.dll"]
LABEL org.opencontainers.image.source="https://github.com/traffiq/ftpCleaner"
