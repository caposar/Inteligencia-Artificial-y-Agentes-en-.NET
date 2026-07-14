# ── Etapa 1: build ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el .csproj y restaurar — activa caché de capas Docker
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar
COPY . .
RUN dotnet publish -c Release --no-restore -o /app/publish

# ── Etapa 2: runtime ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# El ensamblado se llama ServidorMCP.dll — coincide con ServidorMCP.csproj
CMD ["dotnet", "ServidorMCP.dll"]
