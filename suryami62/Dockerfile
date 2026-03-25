FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
RUN apt-get update \
	&& apt-get install -y --no-install-recommends libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["suryami62/suryami62.csproj", "suryami62/"]
COPY ["suryami62.Domain/suryami62.Domain.csproj", "suryami62.Domain/"]
COPY ["suryami62.Infrastructure/suryami62.Infrastructure.csproj", "suryami62.Infrastructure/"]
COPY ["suryami62.Application/suryami62.Application.csproj", "suryami62.Application/"]
RUN dotnet restore "suryami62/suryami62.csproj"
COPY ["suryami62/Components/", "suryami62/Components/"]
COPY ["suryami62/Data/", "suryami62/Data/"]
COPY ["suryami62/Properties/", "suryami62/Properties/"]
COPY ["suryami62/Security/", "suryami62/Security/"]
COPY ["suryami62/Services/", "suryami62/Services/"]
COPY ["suryami62/Styles/", "suryami62/Styles/"]
COPY ["suryami62/wwwroot/", "suryami62/wwwroot/"]
COPY ["suryami62/Program.cs", "suryami62/"]
COPY ["suryami62/appsettings.json", "suryami62/"]
COPY ["suryami62/package.json", "suryami62/"]
COPY ["suryami62.Application/DependencyInjection.cs", "suryami62.Application/"]
COPY ["suryami62.Application/Persistence/", "suryami62.Application/Persistence/"]
COPY ["suryami62.Application/Services/", "suryami62.Application/Services/"]
COPY ["suryami62.Domain/Models/", "suryami62.Domain/Models/"]
COPY ["suryami62.Infrastructure/Data/", "suryami62.Infrastructure/Data/"]
COPY ["suryami62.Infrastructure/Persistence/", "suryami62.Infrastructure/Persistence/"]
COPY ["suryami62.Infrastructure/DependencyInjection.cs", "suryami62.Infrastructure/"]
WORKDIR "/src/suryami62"
RUN dotnet build "./suryami62.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./suryami62.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys /app/wwwroot/img/uploads \
	&& chown -R "$APP_UID:0" /home/app/.aspnet /app/wwwroot/img/uploads \
	&& chmod -R 775 /app/wwwroot/img/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "suryami62.dll"]
