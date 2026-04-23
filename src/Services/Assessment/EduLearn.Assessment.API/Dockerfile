FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Assessment/EduLearn.Assessment.API/EduLearn.Assessment.API.csproj", "src/Services/Assessment/EduLearn.Assessment.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Assessment/EduLearn.Assessment.API/EduLearn.Assessment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Assessment/EduLearn.Assessment.API"
RUN dotnet publish "EduLearn.Assessment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p wwwroot/submissions
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Assessment.API.dll"]
