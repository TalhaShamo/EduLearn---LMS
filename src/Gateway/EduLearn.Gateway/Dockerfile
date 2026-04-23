# Multi-stage Dockerfile — build → publish → runtime
# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY ["src/Gateway/EduLearn.Gateway/EduLearn.Gateway.csproj", "src/Gateway/EduLearn.Gateway/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Gateway/EduLearn.Gateway/EduLearn.Gateway.csproj"

# Copy all source and build
COPY . .
WORKDIR "/src/src/Gateway/EduLearn.Gateway"
RUN dotnet build "EduLearn.Gateway.csproj" -c Release -o /app/build

# Stage 2: Publish optimised output
FROM build AS publish
RUN dotnet publish "EduLearn.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Lean runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Gateway.dll"]
