FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Account/AccountService.Api/AccountService.Api.csproj", "src/Services/Account/AccountService.Api/"]
COPY ["src/Services/Account/AccountService.Application/AccountService.Application.csproj", "src/Services/Account/AccountService.Application/"]
COPY ["src/Services/Account/AccountService.Domain/AccountService.Domain.csproj", "src/Services/Account/AccountService.Domain/"]
COPY ["src/Services/Account/AccountService.Infrastructure/AccountService.Infrastructure.csproj", "src/Services/Account/AccountService.Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/Account/AccountService.Api/AccountService.Api.csproj"

COPY . .

WORKDIR "/src/src/Services/Account/AccountService.Api"
RUN dotnet build "AccountService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "AccountService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccountService.Api.dll"]
