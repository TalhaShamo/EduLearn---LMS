FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Notification/EduLearn.Notification.API/EduLearn.Notification.API.csproj", "src/Services/Notification/EduLearn.Notification.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Notification/EduLearn.Notification.API/EduLearn.Notification.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Notification/EduLearn.Notification.API"
RUN dotnet publish "EduLearn.Notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Notification.API.dll"]
