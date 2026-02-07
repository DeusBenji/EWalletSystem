FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Token/TokenService.Api/TokenService.Api.csproj", "src/Services/Token/TokenService.Api/"]
COPY ["src/Services/Token/TokenService.Application/TokenService.Application.csproj", "src/Services/Token/TokenService.Application/"]
COPY ["src/Services/Token/TokenService.Domain/TokenService.Domain.csproj", "src/Services/Token/TokenService.Domain/"]
COPY ["src/Services/Token/TokenService.Infrastructure/TokenService.Infrastructure.csproj", "src/Services/Token/TokenService.Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/Token/TokenService.Api/TokenService.Api.csproj"

COPY . .

WORKDIR "/src/src/Services/Token/TokenService.Api"
RUN dotnet build "TokenService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "TokenService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TokenService.Api.dll"]
