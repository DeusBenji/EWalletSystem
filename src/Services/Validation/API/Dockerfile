FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Validation/API/Api.csproj", "src/Services/Validation/API/"]
COPY ["src/Services/Validation/Application/Application.csproj", "src/Services/Validation/Application/"]
COPY ["src/Services/Validation/Domain/Domain.csproj", "src/Services/Validation/Domain/"]
COPY ["src/Services/Validation/Infrastructure/Infrastructure.csproj", "src/Services/Validation/Infrastructure/"]
COPY ["src/Shared/Shared/Shared/Shared.csproj", "src/Shared/Shared/Shared/"]

RUN dotnet restore "src/Services/Validation/API/Api.csproj"

COPY . .

WORKDIR "/src/src/Services/Validation/API"
RUN dotnet build "Api.csproj" -c Release -o /app/build
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
