FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiér csproj-filer for IdentityService-laget
COPY ["src/Services/IdentityService/IdentityService/IdentityService.csproj", "src/Services/IdentityService/IdentityService/"]
COPY ["src/Services/IdentityService/IdentityService.Application/IdentityService.Application.csproj", "src/Services/IdentityService/IdentityService.Application/"]
COPY ["src/Services/IdentityService/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/IdentityService.Domain/"]
COPY ["src/Services/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/Services/IdentityService/IdentityService.Infrastructure/"]

# Hvis IdentityService også bruger Shared-projekt, kan du tilføje:

RUN dotnet restore "src/Services/IdentityService/IdentityService/IdentityService.csproj"

# Kopiér resten af løsningen
COPY . .

# Byg og publish IdentityService API
WORKDIR "/src/src/Services/IdentityService/IdentityService"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
