# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ArchoCybo.sln ./
COPY ArchoCybo/ArchoCybo.csproj ArchoCybo/
COPY ArchoCybo.Application/ArchoCybo.Application.csproj ArchoCybo.Application/
COPY ArchoCybo.Infrastructure/ArchoCybo.Infrastructure.csproj ArchoCybo.Infrastructure/
COPY ArchoCybo.Domain/ArchoCybo.Domain.csproj ArchoCybo.Domain/
COPY ArchoCybo.SharedKernel/ArchoCybo.SharedKernel.csproj ArchoCybo.SharedKernel/
RUN dotnet restore ArchoCybo/ArchoCybo.csproj
COPY . .
RUN dotnet publish ArchoCybo/ArchoCybo.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet","ArchoCybo.dll"]
