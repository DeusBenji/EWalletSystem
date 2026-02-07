FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/IdentityService/IdentityService.API/IdentityService.Api.csproj", "src/Services/IdentityService/IdentityService.API/"]
COPY ["src/Services/IdentityService/IdentityService.Application/IdentityService.Application.csproj", "src/Services/IdentityService/IdentityService.Application/"]
COPY ["src/Services/IdentityService/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/IdentityService.Domain/"]
COPY ["src/Services/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/Services/IdentityService/IdentityService.Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/IdentityService/IdentityService.API/IdentityService.Api.csproj"

COPY . .

WORKDIR "/src/src/Services/IdentityService/IdentityService.API"
RUN dotnet build "IdentityService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "IdentityService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
