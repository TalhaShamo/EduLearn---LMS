FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Certificate/EduLearn.Certificate.API/EduLearn.Certificate.API.csproj", "src/Services/Certificate/EduLearn.Certificate.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Certificate/EduLearn.Certificate.API/EduLearn.Certificate.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Certificate/EduLearn.Certificate.API"
RUN dotnet publish "EduLearn.Certificate.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p wwwroot/certificates
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Certificate.API.dll"]
