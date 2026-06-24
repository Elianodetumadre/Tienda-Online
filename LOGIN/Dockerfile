# Molde para compilar el proyecto (Actualizado a .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "LOGIN.csproj"
RUN dotnet publish "LOGIN.csproj" -c Release -o /app/publish

# Molde ligero para correr la página en internet (Actualizado a .NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LOGIN.dll"]