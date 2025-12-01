FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Account/API/AccountService.API.csproj", "src/Services/Account/API/"]
COPY ["src/Services/Account/Application/Application.csproj", "src/Services/Account/Application/"]
COPY ["src/Services/Account/Domain/Domain.csproj", "src/Services/Account/Domain/"]
COPY ["src/Services/Account/Infrastructure/Infrastructure.csproj", "src/Services/Account/Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/Account/API/AccountService.API.csproj"

COPY . .

WORKDIR "/src/src/Services/Account/API"
RUN dotnet build "AccountService.API.csproj" -c Release -o /app/build
RUN dotnet publish "AccountService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccountService.API.dll"]
