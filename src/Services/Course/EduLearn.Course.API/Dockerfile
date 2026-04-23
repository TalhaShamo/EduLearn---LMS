FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Course/EduLearn.Course.API/EduLearn.Course.API.csproj", "src/Services/Course/EduLearn.Course.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Course/EduLearn.Course.API/EduLearn.Course.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Course/EduLearn.Course.API"
RUN dotnet publish "EduLearn.Course.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p wwwroot/videos
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Course.API.dll"]
