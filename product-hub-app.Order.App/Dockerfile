# ...

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["product-hub-app.Order.App/product-hub-app.Order.App.csproj", "product-hub-app.Order.App/"]
RUN dotnet restore "product-hub-app.Order.App/product-hub-app.Order.App.csproj"
COPY . .
WORKDIR "/src/product-hub-app.Order.App"
RUN dotnet build "product-hub-app.Order.App.csproj" -c Debug -o /app/build  # Изменено на режим отладки

FROM build AS publish
RUN dotnet publish "product-hub-app.Order.App.csproj" -c Debug -o /app/publish /p:UseAppHost=false  # Изменено на режим отладки

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "product-hub-app.Order.App.dll"]
