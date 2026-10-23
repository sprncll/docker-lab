# Docker Lab Log

## Part 0 - Environment setup
- docker --version: Docker version 29.9.2, build a82e528
- docker compose version: Docker Compose version v5.6.1
- hello-world: Hello from Docker!
- .NET 8 SDK (dotnet --list-sdks): 8.0.426 [C:\Program Files\dotnet\sdk]

## Part 1 - Prebuilt container
- Image: nginx
- Container: web
- Port mapping: host 8081 -> container 80
- nginx welcome page loaded at http://localhost:8081: True
- docker ps while running:
```
CONTAINER ID   IMAGE     COMMAND                  CREATED         STATUS         PORTS                                     NAMES
0c9c6cb24d6c   nginx     "/docker-entrypoint.ΓÇª"   3 seconds ago   Up 2 seconds   0.0.0.0:8081->80/tcp, [::]:8081->80/tcp   web
```
- Cleaned up with: docker stop web; docker rm web

## Part 2 - API image
- Image: myapi:1.0, size: 346MB
- SDK base (mcr.microsoft.com/dotnet/sdk:8.0) size: 1.24GB
```
REPOSITORY:TAG                               SIZE
myapi:1.0                                    346MB
mcr.microsoft.com/dotnet/sdk:8.0             1.24GB
```
- Startup log: Now listening on: http://[::]:8080
- /api/health from container: {"status":"healthy","host":"a32289b68df1"}

## Part 3 - Compose with SQL Server
- docker compose ps (db healthy):
```
NAME               IMAGE                                        COMMAND                  SERVICE   CREATED         STATUS                   PORTS
docker-lab-api-1   docker-lab-api                               "dotnet MyApi.dll"       api       8 seconds ago   Up 1 second              0.0.0.0:8080->8080/tcp, [::]:8080->8080/tcp
docker-lab-db-1    mcr.microsoft.com/mssql/server:2022-latest   "/opt/mssql/bin/launΓÇª"   db        8 seconds ago   Up 7 seconds (healthy)   1433/tcp
```
- /api/items after docker compose down / up (no -v) - "Persistence test" row survived via the named volume:
```
[{"value":[{"id":1,"name":"Keyboard"},{"id":2,"name":"Mouse"},{"id":3,"name":"Monitor"},{"id":4,"name":"Persistence test"}],"Count":4}]
```

## Part 4 - Full stack and scaling
- Command: docker compose up -d --build --scale api=3
- docker compose ps (nginx, 3x api, db, cache):
```
NAME                 IMAGE                                        COMMAND                  SERVICE   CREATED          STATUS                    PORTS
docker-lab-api-1     docker-lab-api                               "dotnet MyApi.dll"       api       26 seconds ago   Up 17 seconds             8080/tcp
docker-lab-api-2     docker-lab-api                               "dotnet MyApi.dll"       api       26 seconds ago   Up 18 seconds             8080/tcp
docker-lab-api-3     docker-lab-api                               "dotnet MyApi.dll"       api       26 seconds ago   Up 17 seconds             8080/tcp
docker-lab-cache-1   redis:7                                      "docker-entrypoint.sΓÇª"   cache     26 seconds ago   Up 25 seconds             6379/tcp
docker-lab-db-1      mcr.microsoft.com/mssql/server:2022-latest   "/opt/mssql/bin/launΓÇª"   db        26 seconds ago   Up 25 seconds (healthy)   1433/tcp
docker-lab-nginx-1   nginx:latest                                 "/docker-entrypoint.ΓÇª"   nginx     26 seconds ago   Up 17 seconds             0.0.0.0:80->80/tcp, [::]:80->80/tcp
```
- 9 calls to http://localhost/api/health were served by 3 different replicas: dc721100e421, dc721100e421, 156cf421e80a, dc721100e421, 3efd442a54e6, 156cf421e80a, dc721100e421, 3efd442a54e6, 156cf421e80a
- Requests spread across replicas (docker compose logs api):
```
api-2  |       Request starting HTTP/1.1 GET http://localhost/api/health - - -
api-2  |       Request starting HTTP/1.1 GET http://localhost/api/health - - -
api-2  |       Request starting HTTP/1.1 GET http://localhost/api/items - - -
api-3  |       Request starting HTTP/1.1 GET http://localhost/api/health - - -
api-3  |       Request starting HTTP/1.1 GET http://localhost/api/health - - -
api-3  |       Request starting HTTP/1.1 GET http://localhost/api/health - - -
```
- /api/items through nginx: [{"value":[{"id":1,"name":"Keyboard"},{"id":2,"name":"Mouse"},{"id":3,"name":"Monitor"},{"id":4,"name":"Persistence test"}],"Count":4}]
- Direct call to http://localhost:8080: Unable to connect - API is not published to the host, only reachable via nginx on port 80
- docker stats --no-stream:
```
NAME                 CPU %     MEM USAGE / LIMIT
docker-lab-nginx-1   0.00%     3.816MiB / 3.69GiB
docker-lab-api-2     0.02%     54.37MiB / 3.69GiB
docker-lab-api-3     0.03%     46.08MiB / 3.69GiB
docker-lab-api-1     0.01%     46.55MiB / 3.69GiB
docker-lab-cache-1   0.49%     3.934MiB / 3.69GiB
docker-lab-db-1      12.14%    731.8MiB / 3.69GiB
```

## Reflection
The multi-stage build let me compile with the full .NET SDK but ship only the ASP.NET runtime, so the final myapi image was 346MB instead of the 1.24GB SDK image, and it contains no compiler or source code. That makes it faster to build, push and start, and gives it a smaller attack surface. The SQL Server health check combined with depends_on: condition: service_healthy made the API wait until the database could actually answer queries, not just until its container had started, which removed the startup race where the API fails to connect. Together with service-name networking (db, api) and the named volume, the whole stack came up reliably with one command, kept its data across restarts, and scaled to three API replicas behind nginx.
