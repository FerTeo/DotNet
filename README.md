# OSSocial



```bash
docker compose up -d
```

```bash
docker compose build
```

```
dotnet tool install --global dotnet-ef
```



in docker exec app

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 9.0.0

dotnet ef migrations add InitMigration

dotnet ef database update

```
