# Molde para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "LOGIN.csproj"
RUN dotnet publish "LOGIN.csproj" -c Release -o /app/publish

# Molde ligero para correr la página en internet
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LOGIN.dll"]# Molde para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "LOGIN.csproj"
RUN dotnet publish "LOGIN.csproj" -c Release -o /app/publish

# Molde ligero para correr la página en internet
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LOGIN.dll"]