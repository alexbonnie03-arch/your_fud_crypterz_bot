FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Crypter.csproj"
WORKDIR "/src"
RUN dotnet build "Crypter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Crypter.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Crypter.dll"]
