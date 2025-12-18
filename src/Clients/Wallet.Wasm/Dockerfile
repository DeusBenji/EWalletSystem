FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Clients/Wallet.Wasm/Wallet.Wasm.csproj", "src/Clients/Wallet.Wasm/"]
RUN dotnet restore "src/Clients/Wallet.Wasm/Wallet.Wasm.csproj"
COPY . .
WORKDIR "/src/src/Clients/Wallet.Wasm"
RUN dotnet build "Wallet.Wasm.csproj" -c Release -o /app/build
RUN dotnet publish "Wallet.Wasm.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY src/Clients/Wallet.Wasm/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
