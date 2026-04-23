FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Payment/EduLearn.Payment.API/EduLearn.Payment.API.csproj", "src/Services/Payment/EduLearn.Payment.API/"]
COPY ["src/Shared/EduLearn.Shared/EduLearn.Shared.csproj", "src/Shared/EduLearn.Shared/"]
RUN dotnet restore "src/Services/Payment/EduLearn.Payment.API/EduLearn.Payment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Payment/EduLearn.Payment.API"
RUN dotnet publish "EduLearn.Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EduLearn.Payment.API.dll"]
