# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Shared.Common/Shared.Common.csproj", "Shared.Common/"]
COPY ["LeaveManagementService/LeaveManagementService.csproj", "LeaveManagementService/"]
RUN dotnet restore "LeaveManagementService/LeaveManagementService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/LeaveManagementService"
RUN dotnet build "LeaveManagementService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LeaveManagementService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaveManagementService.dll"]
