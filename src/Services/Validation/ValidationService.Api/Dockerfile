FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Validation/ValidationService.Api/ValidationService.Api.csproj", "src/Services/Validation/ValidationService.Api/"]
COPY ["src/Services/Validation/ValidationService.Application/ValidationService.Application.csproj", "src/Services/Validation/ValidationService.Application/"]
COPY ["src/Services/Validation/ValidationService.Domain/ValidationService.Domain.csproj", "src/Services/Validation/ValidationService.Domain/"]
COPY ["src/Services/Validation/ValidationService.Infrastructure/ValidationService.Infrastructure.csproj", "src/Services/Validation/ValidationService.Infrastructure/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]

RUN dotnet restore "src/Services/Validation/ValidationService.Api/ValidationService.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Validation/ValidationService.Api"
RUN dotnet build "ValidationService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "ValidationService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ValidationService.Api.dll"]
