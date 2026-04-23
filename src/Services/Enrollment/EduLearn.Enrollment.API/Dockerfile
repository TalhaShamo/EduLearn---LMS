FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Enrollment/EduLearn.Enrollment.API/EduLearn.Enrollment.API.csproj", "src/Services/Enrollment/EduLearn.Enrollment.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Enrollment/EduLearn.Enrollment.API/EduLearn.Enrollment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Enrollment/EduLearn.Enrollment.API"
RUN dotnet publish "EduLearn.Enrollment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Enrollment.API.dll"]
