FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["suryami62/suryami62.csproj", "suryami62/"]
COPY ["suryami62.Domain/suryami62.Domain.csproj", "suryami62.Domain/"]
COPY ["suryami62.Infrastructure/suryami62.Infrastructure.csproj", "suryami62.Infrastructure/"]
COPY ["suryami62.Application/suryami62.Application.csproj", "suryami62.Application/"]
RUN dotnet restore "suryami62/suryami62.csproj"
COPY . .
WORKDIR "/src/suryami62"
RUN dotnet build "./suryami62.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./suryami62.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "suryami62.dll"]
