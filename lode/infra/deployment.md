# Deployment: Oracle Cloud VM + GitHub Actions CI/CD

## Overview
BoxTracker deploys to an Oracle Cloud Compute Instance (Melbourne, Free Tier, Shape: VM.Standard.E2.1.Micro, 1/8 OCPU, 1GB RAM) with automatic CI/CD via GitHub Actions. Backend runs as a systemd service; frontend served via nginx with basic auth. All deployment happens via SSH key-based authentication with a restricted `deploy` user.

## Infrastructure

### VM Details
- **Host**: `158.179.31.108` (Public IP, ephemeral)
- **Internal IP**: `10.0.0.156`
- **Region**: Australia Southeast (Melbourne)
- **OS**: Ubuntu 22.04 LTS
- **Hostname**: melbourne-micro-free

### Runtime Stack
- **.NET 10.0 SDK + ASP.NET Core 10.0 Runtime** at `/opt/dotnet`
- **nginx 1.18.0** (reverse proxy + static file server + basic auth)
- **systemd** (process management)

## Server Setup (One-Time Manual)

### 1. Install .NET Runtimes
Both base .NET and ASP.NET Core runtimes are required:
```bash
# Base .NET runtime
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime dotnet --version 10.0.0 --install-dir /opt/dotnet

# ASP.NET Core runtime (required for Saturn web server)
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime aspnetcore --version 10.0.0 --install-dir /opt/dotnet

# Verify both are installed
/opt/dotnet/dotnet --list-runtimes
# Output: Microsoft.AspNetCore.App 10.0.0 [...]
#         Microsoft.NETCore.App 10.0.0 [...]
```

### 2. Create Deployment User
```bash
sudo useradd --system --create-home --shell /bin/bash deploy
sudo mkdir -p /opt/boxtracker/{server,frontend,data/photos}
sudo chown -R deploy:deploy /opt/boxtracker
```

### 3. SSH Key Setup
Generate on local machine:
```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/boxtracker_deploy -N ""
```

Add public key to VM:
```bash
sudo mkdir -p /home/deploy/.ssh
sudo nano /home/deploy/.ssh/authorized_keys  # paste public key
sudo chown -R deploy:deploy /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh && sudo chmod 600 /home/deploy/.ssh/authorized_keys
```

### 4. Sudo Configuration
Allow `deploy` user passwordless sudo for systemd control only:
```bash
# File: /etc/sudoers.d/deploy-boxtracker
deploy ALL=(ALL) NOPASSWD: /bin/systemctl restart boxtracker, /bin/systemctl start boxtracker, /bin/systemctl stop boxtracker
```

### 5. Basic Auth Setup
```bash
sudo apt-get install -y apache2-utils
sudo htpasswd -cb /etc/nginx/.htpasswd daniel 8busstop
sudo chmod 640 /etc/nginx/.htpasswd
sudo chown root:www-data /etc/nginx/.htpasswd
```

### 6. nginx Configuration
File: `/etc/nginx/sites-available/boxtracker`
```nginx
server {
    listen 80;
    server_name 158.179.31.108;
    root /opt/boxtracker/frontend/dist;
    index index.html;

    auth_basic "BoxTracker";
    auth_basic_user_file /etc/nginx/.htpasswd;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

Enable and test:
```bash
sudo ln -s /etc/nginx/sites-available/boxtracker /etc/nginx/sites-enabled/
sudo unlink /etc/nginx/sites-enabled/default  # disable default site
sudo nginx -t && sudo systemctl reload nginx
```

### 7. systemd Service
File: `/etc/systemd/system/boxtracker.service`
```ini
[Unit]
Description=BoxTracker Server
After=network.target

[Service]
Type=simple
User=deploy
WorkingDirectory=/opt/boxtracker/server
ExecStart=/opt/dotnet/dotnet BoxTracker.Server.dll --urls http://localhost:5000
Restart=always
RestartSec=5
Environment=BOXTRACKER_DATA=/opt/boxtracker/data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable:
```bash
sudo systemctl daemon-reload && sudo systemctl enable boxtracker
```

### 8. iptables Configuration
**Critical**: Oracle Cloud's default iptables rejects all traffic except SSH. Must explicitly allow ports 80 and 5000:
```bash
sudo iptables -I INPUT 5 -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -p tcp --dport 5000 -j ACCEPT
sudo apt-get install -y iptables-persistent
sudo netfilter-persistent save
```

**Why this was needed**: Oracle Cloud's `free-tier-iptables` template has a final `REJECT all` rule. Security Groups control network-level filtering; iptables controls host-level filtering. Both must allow the traffic.

## Deployment Process

### GitHub Actions Workflow (`.github/workflows/ci.yml`)
- **Test Job**: Runs xUnit tests on every commit, both `Domain.Tests` and `Api.Tests`
- **Build Job**: Depends on test; runs Fable + Vite, publishes binaries for linux-x64 and linux-arm64
- **Deploy Job**: Triggered on pushes to `main` (not PRs); uses SSH to:
  1. Download build artifacts
  2. `rsync` server binaries to `/opt/boxtracker/server/` on VM
  3. `rsync` frontend dist to `/opt/boxtracker/frontend/` on VM
  4. SSH in and run `sudo systemctl restart boxtracker`

### GitHub Secrets Required
| Secret | Value |
|--------|-------|
| `DEPLOY_SSH_KEY` | Private key from `~/.ssh/boxtracker_deploy` |
| `DEPLOY_HOST` | `158.179.31.108` |
| `DEPLOY_USER` | `deploy` |

### Deployment from CLI
```bash
# Manually push to main (CI/CD triggers automatically)
git push origin main

# SSH in to check service status
ssh -i ~/.ssh/boxtracker_deploy deploy@158.179.31.108
sudo systemctl status boxtracker
```

## Network Topology

```
External User
    ↓ HTTP:80 (public IP 158.179.31.108)
Oracle Cloud Security Group
    ↓ (traffic routed to internal IP 10.0.0.156:80)
iptables (INPUT rule: ACCEPT tcp --dport 80)
    ↓
nginx (listen 80; basic auth)
    ├─ Static files: /opt/boxtracker/frontend/dist/
    └─ API requests (/api/*): proxy to localhost:5000
         ↓
    dotnet (Saturn backend, listen localhost:5000)
         ↓
    /opt/boxtracker/data/boxtracker.db (SQLite)
    /opt/boxtracker/data/photos/ (photo files)
```

## Troubleshooting

### Service Won't Start
Check logs via:
```bash
sudo journalctl -u boxtracker -n 50
# Deploy user can't read systemd journal — need ubuntu user or root
```

### Site Returns 403 Forbidden
- Check nginx root points to `/opt/boxtracker/frontend/dist/` (not `/opt/boxtracker/frontend/`)
- Verify frontend files exist: `ls /opt/boxtracker/frontend/dist/`
- Check htpasswd: `sudo htpasswd -i /etc/nginx/.htpasswd daniel` (interactively verify password)

### Connection Timeouts
- Verify iptables rules: `sudo iptables -L INPUT -n`
- Must see `ACCEPT tcp -- 0.0.0.0/0 0.0.0.0/0 tcp dpt:80`
- Verify nginx is listening: `sudo lsof -i :80`
- Monitor incoming traffic: `sudo tcpdump -i ens3 port 80`

### Backend Can't Connect to Localhost
Saturn listens on `localhost:5000` (127.0.0.1 + ::1). nginx proxy on same VM can reach it. Check no firewall rules block 127.0.0.1 traffic.

## Access
- **Frontend**: http://158.179.31.108 (username: `daniel`, password: `8busstop`)
- **Direct API**: http://158.179.31.108/api/locations (with basic auth)

## Key Invariants
- Frontend dist is deployed to `/opt/boxtracker/frontend/dist/`, not `/opt/boxtracker/frontend/`
- nginx root must point to the `dist/` directory for Vite output to be served
- Both .NET (base) and ASP.NET Core runtimes are required — base alone is insufficient
- iptables must allow port 80 and 5000 in addition to Oracle Cloud security groups
- Basic auth applies to all locations (frontend + API) due to inheritance in nginx config
- `deploy` user is restricted to systemctl commands only; cannot install packages or modify system files
