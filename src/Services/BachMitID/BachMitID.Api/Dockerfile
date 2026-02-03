FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiér csproj-filer for BachMitID-laget
COPY ["src/Services/BachMitID/BachMitID/BachMitID.csproj", "src/Services/BachMitID/BachMitID/"]
COPY ["src/Services/BachMitID/BachMitID.Application/BachMitID.Application.csproj", "src/Services/BachMitID/BachMitID.Application/"]
COPY ["src/Services/BachMitID/BachMitID.Domain/BachMitID.Domain.csproj", "src/Services/BachMitID/BachMitID.Domain/"]
COPY ["src/Services/BachMitID/BachMitID.Infrastructure/BachMitID.Infrastructure.csproj", "src/Services/BachMitID/BachMitID.Infrastructure/"]

# Hvis BachMitID også bruger Shared-projekt, kan du tilføje:

RUN dotnet restore "src/Services/BachMitID/BachMitID/BachMitID.csproj"

# Kopiér resten af løsningen
COPY . .

# Byg og publish BachMitID API
WORKDIR "/src/src/Services/BachMitID/BachMitID"
RUN dotnet build "BachMitID.csproj" -c Release -o /app/build
RUN dotnet publish "BachMitID.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BachMitID.dll"]
