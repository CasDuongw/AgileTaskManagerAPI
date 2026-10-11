# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file và restore dependencies
COPY ["AgileTaskManagerAPI/AgileTaskManagerAPI.csproj", "AgileTaskManagerAPI/"]
RUN dotnet restore "AgileTaskManagerAPI/AgileTaskManagerAPI.csproj"

# Copy source code và publish Release
COPY AgileTaskManagerAPI/ AgileTaskManagerAPI/
WORKDIR "/src/AgileTaskManagerAPI"
RUN dotnet publish "AgileTaskManagerAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình cổng cho Render (.NET 8 mặc định 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgileTaskManagerAPI.dll"]
