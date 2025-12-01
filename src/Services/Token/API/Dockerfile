FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Token/API/Api.csproj", "src/Services/Token/API/"]
COPY ["src/Services/Token/Application/Application.csproj", "src/Services/Token/Application/"]
COPY ["src/Services/Token/Domain/Domain.csproj", "src/Services/Token/Domain/"]
COPY ["src/Services/Token/Infrastructure/Infrastructure.csproj", "src/Services/Token/Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/Token/API/Api.csproj"

COPY . .

WORKDIR "/src/src/Services/Token/API"
RUN dotnet build "Api.csproj" -c Release -o /app/build
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]