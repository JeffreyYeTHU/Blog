# 使用nexus作为本地repository

```bash
# run image
docker run -d -p 8081:8081 -p 8082:8082 -p 8083:8083 -v /opt/my-nexus-data:/nexus-data --name my-nexus sonatype/nexus3:3.0.0
```