FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Identity/EduLearn.Identity.API/EduLearn.Identity.API.csproj", "src/Services/Identity/EduLearn.Identity.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Identity/EduLearn.Identity.API/EduLearn.Identity.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Identity/EduLearn.Identity.API"
RUN dotnet build "EduLearn.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Identity.API.dll"]
