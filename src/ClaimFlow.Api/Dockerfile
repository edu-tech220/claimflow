FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ClaimFlow.sln", "./"]
COPY ["src/ClaimFlow.Api/ClaimFlow.Api.csproj", "src/ClaimFlow.Api/"]
COPY ["src/ClaimFlow.Web/ClaimFlow.Web.csproj", "src/ClaimFlow.Web/"]
COPY ["tests/ClaimFlow.Api.Tests/ClaimFlow.Api.Tests.csproj", "tests/ClaimFlow.Api.Tests/"]
RUN dotnet restore "src/ClaimFlow.Api/ClaimFlow.Api.csproj"

COPY . .
WORKDIR /src/src/ClaimFlow.Api
RUN dotnet publish ClaimFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClaimFlow.Api.dll"]
