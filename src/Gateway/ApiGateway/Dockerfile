FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopiér csproj så restore kan køres separat (hurtigere builds)
COPY ["src/Gateway/ApiGateway/ApiGateway.csproj", "src/Gateway/ApiGateway/"]

RUN dotnet restore "src/Gateway/ApiGateway/ApiGateway.csproj"

# Kopiér resten af løsningen ind
COPY . .

# Byg og publish ApiGateway
WORKDIR "/src/src/Gateway/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
