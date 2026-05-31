# 1. المرحلة الأولى: بناء المشروع (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# نسخ ملفات المشروع وعمل Restore
COPY *.csproj ./
RUN dotnet restore

# نسخ باقي الكود وعمل Build
COPY . ./
RUN dotnet publish -c Release -o out

# 2. المرحلة الثانية: تشغيل المشروع (Runtime Stage)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# تشغيل الـ API
ENTRYPOINT ["dotnet", "TeacherOS.API.dll"]