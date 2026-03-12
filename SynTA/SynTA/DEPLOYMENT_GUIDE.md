# SynTA - Deployment Guide

## Overview
This guide provides step-by-step instructions for deploying the SynTA application to production using Digital Ocean and Docker.

## Prerequisites

### Required Accounts
- Digital Ocean account
- Domain name (optional but recommended)
- GitHub account (for CI/CD)

### Required Software
- Docker Desktop
- .NET 10 SDK
- Git
- SQL Server (or Azure SQL Database)

### Required Configuration
- OpenAI API Key (or Gemini API Key)
- SMTP server for email (optional)

## Environment Setup

### 1. Environment Variables

Create a `.env` file or configure the following environment variables:

```bash
# Database
ConnectionStrings__DefaultConnection="Server=your-server;Database=SynTADb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"

# OpenAI Configuration
OpenAI__ApiKey="your-openai-api-key"
OpenAI__Model="gpt-4"

# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443

# Admin Account (Change these!)
AdminAccount__Email="admin@yourdomain.com"
AdminAccount__Password="SecurePassword123!"

# Email Configuration (optional)
Email__SmtpServer="smtp.gmail.com"
Email__SmtpPort=587
Email__Username="your-email@gmail.com"
Email__Password="your-email-password"
```

### 2. Docker Configuration

#### Dockerfile
The application should already have a Dockerfile. If not, create one:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SynTA/SynTA.csproj", "SynTA/"]
RUN dotnet restore "SynTA/SynTA.csproj"
COPY . .
WORKDIR "/src/SynTA"
RUN dotnet build "SynTA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SynTA.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SynTA.dll"]
```

#### docker-compose.yml
```yaml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: SynTA/Dockerfile
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - OpenAI__ApiKey=${OPENAI_API_KEY}
    depends_on:
      - db
    restart: unless-stopped

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    restart: unless-stopped

volumes:
  sqldata:
```

## Digital Ocean Deployment

### Option 1: Using Digital Ocean App Platform

1. **Create a New App**:
   ```bash
   # Push your code to GitHub
   git push origin main
   ```

2. **Configure App in Digital Ocean**:
   - Go to Digital Ocean App Platform
   - Click "Create App"
   - Connect your GitHub repository
   - Select the branch to deploy (main/develop)

3. **Configure Environment Variables**:
   - Add all environment variables from the `.env` file
   - Mark sensitive variables as "encrypted"

4. **Configure Database**:
   - Create a managed database (SQL Server or PostgreSQL)
   - Update connection string environment variable

5. **Deploy**:
   - Review settings
   - Click "Create Resources"
   - Wait for deployment to complete

### Option 2: Using Digital Ocean Droplets with Docker

1. **Create a Droplet**:
   ```bash
   # Size: At least 2GB RAM
   # OS: Ubuntu 22.04 LTS
   # Add SSH key for access
   ```

2. **Connect to Droplet**:
   ```bash
   ssh root@your-droplet-ip
   ```

3. **Install Docker**:
   ```bash
   # Update package index
   apt update
   apt upgrade -y

   # Install Docker
   curl -fsSL https://get.docker.com -o get-docker.sh
   sh get-docker.sh

   # Install Docker Compose
   apt install docker-compose -y

   # Verify installation
   docker --version
   docker-compose --version
   ```

4. **Clone Repository**:
   ```bash
   # Install Git
   apt install git -y

   # Clone your repository
   git clone https://github.com/vancoder1/SynTA.git
   cd SynTA
   ```

5. **Configure Environment**:
   ```bash
   # Create .env file
   nano .env

   # Add all environment variables
   # Save and exit (Ctrl+X, Y, Enter)
   ```

6. **Build and Run**:
   ```bash
   # Build Docker images
   docker-compose build

   # Start containers
   docker-compose up -d

   # Check status
   docker-compose ps

   # View logs
   docker-compose logs -f web
   ```

7. **Setup Database**:
   ```bash
   # Run migrations
   docker-compose exec web dotnet ef database update

   # Or manually run SQL scripts if needed
   ```

### Option 3: Using Kubernetes (Advanced)

For high-availability deployments:

```yaml
# kubernetes-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: synta-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: synta
  template:
    metadata:
      labels:
        app: synta
    spec:
      containers:
      - name: synta
        image: your-registry/synta:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: synta-secrets
              key: db-connection
```

## SSL/TLS Configuration

### Using Let's Encrypt with Nginx

1. **Install Nginx**:
   ```bash
   apt install nginx -y
   ```

2. **Configure Nginx**:
   ```bash
   nano /etc/nginx/sites-available/synta
   ```

   ```nginx
   server {
       listen 80;
       server_name yourdomain.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

3. **Enable Site**:
   ```bash
   ln -s /etc/nginx/sites-available/synta /etc/nginx/sites-enabled/
   nginx -t
   systemctl restart nginx
   ```

4. **Install Certbot**:
   ```bash
   apt install certbot python3-certbot-nginx -y
   certbot --nginx -d yourdomain.com
   ```

## Database Configuration

### SQL Server Setup

1. **Create Database**:
   ```sql
   CREATE DATABASE SynTADb;
   GO

   USE SynTADb;
   GO
   ```

2. **Run Migrations**:
   ```bash
   # From your local machine
   dotnet ef database update --project SynTA

   # Or from the container
   docker-compose exec web dotnet ef database update
   ```

3. **Backup Strategy**:
   ```bash
   # Create backup script
   cat > /root/backup-db.sh << 'EOF'
   #!/bin/bash
   DATE=$(date +%Y%m%d_%H%M%S)
   docker-compose exec -T db /opt/mssql-tools/bin/sqlcmd \
     -S localhost -U SA -P "$DB_PASSWORD" \
     -Q "BACKUP DATABASE [SynTADb] TO DISK = N'/var/opt/mssql/backup/SynTADb_$DATE.bak'"
   EOF

   chmod +x /root/backup-db.sh

   # Schedule daily backups
   crontab -e
   # Add: 0 2 * * * /root/backup-db.sh
   ```

## Monitoring and Logging

### Application Insights (Azure)
Add to `Program.cs`:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog for File Logging
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("/var/log/synta/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

app.MapHealthChecks("/health");
```

## CI/CD Pipeline

### GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '10.0.x'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Test
      run: dotnet test
    
    - name: Deploy to Digital Ocean
      uses: digitalocean/action-doctl@v2
      with:
        token: ${{ secrets.DIGITALOCEAN_ACCESS_TOKEN }}
    
    - name: Build and push Docker image
      run: |
        docker build -t registry.digitalocean.com/your-registry/synta:latest .
        docker push registry.digitalocean.com/your-registry/synta:latest
    
    - name: Update deployment
      run: |
        ssh root@your-droplet-ip "cd /app/SynTA && git pull && docker-compose up -d --build"
```

## Security Checklist

- [ ] Change default admin password
- [ ] Use strong database passwords
- [ ] Enable HTTPS/SSL
- [ ] Configure firewall rules
- [ ] Set up rate limiting
- [ ] Enable CORS properly
- [ ] Secure API keys in environment variables
- [ ] Regular security updates
- [ ] Configure Content Security Policy
- [ ] Enable HSTS headers

## Performance Optimization

### Application Settings
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxRequestBodySize": 10485760
    }
  }
}
```

### Caching
```csharp
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();
```

### Database Indexing
Ensure proper indexes are created (already done in `ApplicationDbContext`):
- UserId on Projects
- ProjectId on UserStories
- UserStoryId on GherkinScenarios and CypressScripts

## Troubleshooting

### Common Issues

**Issue**: Application won't start
```bash
# Check logs
docker-compose logs web

# Check environment variables
docker-compose exec web printenv

# Verify database connection
docker-compose exec web dotnet ef database update
```

**Issue**: Database connection fails
```bash
# Test SQL Server connectivity
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "$DB_PASSWORD"

# Check connection string
echo $ConnectionStrings__DefaultConnection
```

**Issue**: High memory usage
```bash
# Check container stats
docker stats

# Restart containers
docker-compose restart
```

## Maintenance

### Regular Tasks

**Daily**:
- Monitor application logs
- Check error rates
- Verify backups completed

**Weekly**:
- Review disk usage
- Check for security updates
- Analyze performance metrics

**Monthly**:
- Update dependencies
- Review user feedback
- Optimize database
- Test disaster recovery

### Update Procedure

1. **Test updates in staging**
2. **Create database backup**
3. **Pull latest code**:
   ```bash
   git pull origin main
   ```
4. **Build and deploy**:
   ```bash
   docker-compose build
   docker-compose up -d
   ```
5. **Run migrations**:
   ```bash
   docker-compose exec web dotnet ef database update
   ```
6. **Verify deployment**:
   ```bash
   curl https://yourdomain.com/health
   ```

## Support and Documentation

- **Application Logs**: `/var/log/synta/`
- **Docker Logs**: `docker-compose logs`
- **Health Check**: `https://yourdomain.com/health`
- **Admin Dashboard**: `https://yourdomain.com/Admin/Dashboard`

## Rollback Procedure

If deployment fails:

1. **Revert to previous version**:
   ```bash
   git revert HEAD
   docker-compose up -d --build
   ```

2. **Restore database backup**:
   ```bash
   docker-compose exec db /opt/mssql-tools/bin/sqlcmd \
     -S localhost -U SA -P "$DB_PASSWORD" \
     -Q "RESTORE DATABASE [SynTADb] FROM DISK = N'/var/opt/mssql/backup/SynTADb_TIMESTAMP.bak'"
   ```

3. **Verify application**:
   ```bash
   curl https://yourdomain.com/health
   ```

## Conclusion

This deployment guide covers the essential steps for getting SynTA into production. Adjust configurations based on your specific requirements and scale. Always test in a staging environment before deploying to production.

For additional help, refer to:
- Digital Ocean Documentation
- Docker Documentation
- ASP.NET Core Deployment Guide
