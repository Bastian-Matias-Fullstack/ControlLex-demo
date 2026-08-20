FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["API.csproj", "./"]
COPY ["Aplicacion/Aplicacion.csproj", "Aplicacion/"]
COPY ["Dominio/Dominio.csproj", "Dominio/"]
COPY ["Infraestructura/Infraestructura.csproj", "Infraestructura/"]
RUN dotnet restore "./API.csproj"

COPY . .
RUN dotnet publish "./API.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet API.dll --urls http://0.0.0.0:${PORT:-8080}"]
