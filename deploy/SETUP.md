# BoxTracker Server Setup Guide

This guide walks through setting up the Oracle VM (158.179.31.108) to run BoxTracker with auto-deployments from GitHub Actions.

## Prerequisites
- SSH access to the server as the `ubuntu` user
- A GitHub repository set up with this codebase
- An SSH ed25519 keypair for deployment (see step 3)

---

## Step 1: Install .NET 10 Runtime and Caddy

SSH into the server and run:

```bash
# Install .NET 10 runtime
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime dotnet --version 10.0.0 --install-dir /opt/dotnet
echo 'export PATH=$PATH:/opt/dotnet' | sudo tee /etc/profile.d/dotnet.sh
source /etc/profile.d/dotnet.sh

# Install Caddy
sudo apt update
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update && sudo apt install -y caddy
```

Verify the installation:
```bash
/opt/dotnet/dotnet --version
caddy version
```

---

## Step 2: Create the `deploy` User and Directories

```bash
sudo useradd --system --create-home --shell /bin/bash deploy
sudo mkdir -p /opt/boxtracker/{server,frontend,data/photos}
sudo chown -R deploy:deploy /opt/boxtracker
```

---

## Step 3: Set Up SSH Key for GitHub Actions

On your **local machine**, generate an ed25519 keypair:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/boxtracker_deploy -N ""
```

This creates:
- `~/.ssh/boxtracker_deploy` (private key) → store as GitHub Secret `DEPLOY_SSH_KEY`
- `~/.ssh/boxtracker_deploy.pub` (public key) → copy to the server

On the **server**:

```bash
sudo mkdir -p /home/deploy/.ssh
sudo bash -c 'cat >> /home/deploy/.ssh/authorized_keys' << EOF
<paste the contents of ~/.ssh/boxtracker_deploy.pub here>
EOF

sudo chown -R deploy:deploy /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh && sudo chmod 600 /home/deploy/.ssh/authorized_keys
```

Test the connection:
```bash
ssh -i ~/.ssh/boxtracker_deploy deploy@158.179.31.108 whoami
# Should output: deploy
```

---

## Step 4: Configure sudo for the `deploy` User

Create `/etc/sudoers.d/deploy-boxtracker`:

```bash
sudo bash -c 'cat > /etc/sudoers.d/deploy-boxtracker' << EOF
deploy ALL=(ALL) NOPASSWD: /bin/systemctl restart boxtracker, /bin/systemctl start boxtracker, /bin/systemctl stop boxtracker
EOF

sudo chmod 440 /etc/sudoers.d/deploy-boxtracker
```

Verify:
```bash
sudo visudo -c -f /etc/sudoers.d/deploy-boxtracker
# Should output: parsed OK
```

---

## Step 5: Create the systemd Service

Copy the `boxtracker.service` file from this repo to the server and enable it:

```bash
sudo cp boxtracker.service /etc/systemd/system/boxtracker.service
sudo systemctl daemon-reload
sudo systemctl enable boxtracker
```

Start it to verify:
```bash
sudo systemctl start boxtracker
sudo systemctl status boxtracker
# Should show: active (running)
```

If there are issues, check the logs:
```bash
sudo journalctl -u boxtracker -n 50
```

---

## Step 6: Set Up Caddy with HTTPS

### 6A: Open port 443 in Oracle Cloud firewall

**In the Oracle Cloud Console:**
1. Navigate to **VCN → Security Lists → Default**
2. Add an ingress rule:
   - Protocol: TCP
   - Source: 0.0.0.0/0
   - Destination Port: 443
3. Save the rule

**On the VM (iptables):**
```bash
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

Verify port 80 is already open:
```bash
sudo iptables -L INPUT -n --line-numbers | grep -E '80|443'
```

### 6B: Point DuckDNS at your server

From anywhere (or from the server), update DuckDNS to map `movingstuff.duckdns.org` to `158.179.31.108`:

```bash
curl "https://www.duckdns.org/update?domains=movingstuff&token=<YOUR_DUCKDNS_TOKEN>&ip=158.179.31.108"
# Should return: OK
```

Verify DNS resolution:
```bash
dig movingstuff.duckdns.org
# Should show: 158.179.31.108
```

### 6C: Configure Caddyfile

Copy the `Caddyfile` from this repo to the server:

```bash
sudo cp Caddyfile /etc/caddy/Caddyfile
```

Before continuing, generate the bcrypt hash for the password `8busstop`:

```bash
caddy hash-password --plaintext '8busstop'
```

This outputs a hash like `$2a$14$...`. Edit `/etc/caddy/Caddyfile` and replace `<HASH-FROM-CADDY-HASH-PASSWORD>` with the hash:

```bash
sudo nano /etc/caddy/Caddyfile
```

Validate the Caddyfile:
```bash
sudo caddy validate --config /etc/caddy/Caddyfile
```

### 6D: Enable Caddy and disable nginx

```bash
sudo systemctl enable caddy
sudo systemctl start caddy
sudo systemctl disable nginx
sudo systemctl stop nginx
```

Check Caddy status and logs:
```bash
sudo systemctl status caddy
sudo journalctl -u caddy -f  # Watch for TLS cert provisioning
```

Caddy will automatically provision a Let's Encrypt certificate via HTTP-01 challenge on port 80. You should see a log line like: `"tls.obtain" cert_name=movingstuff.duckdns.org`

---

## Step 7: Add GitHub Secrets

In your GitHub repository, go to **Settings → Secrets and variables → Actions** and add:

| Secret Name | Value |
|---|---|
| `DEPLOY_SSH_KEY` | Contents of `~/.ssh/boxtracker_deploy` (the private key) |
| `DEPLOY_HOST` | `158.179.31.108` |
| `DEPLOY_USER` | `deploy` |

---

## Step 8: Test the Deployment

Make a test commit and push to `main`:

```bash
git add .
git commit -m "test: trigger CI/CD pipeline"
git push origin main
```

Watch the GitHub Actions tab in your repository:
1. The `test` job should pass
2. The `build` job should pass
3. The `deploy` job should pass

If the deploy job fails, check:
- The SSH key is correctly added to GitHub Secrets
- The `deploy` user exists and can be SSH'd into
- The `/opt/boxtracker/` directories are writable by the `deploy` user

---

## Step 9: Verify the App is Running

### Via HTTPS (recommended)

Visit `https://movingstuff.duckdns.org` in your browser:
- You should be prompted for a username/password
- Login with: `daniel` / `8busstop`
- The BoxTracker frontend should load
- Navigate around and verify API calls work (check browser console for errors)
- Check the certificate: click the lock icon → "Certificate is valid"

### Via curl (for automated verification)

```bash
curl -u daniel:8busstop https://movingstuff.duckdns.org/api/boxes
# Should return JSON array of boxes
```

Check the certificate details:
```bash
echo | openssl s_client -connect movingstuff.duckdns.org:443 2>/dev/null | openssl x509 -noout -issuer -dates
# Should show: issuer=C = US, O = Let's Encrypt, ...
```

---

## Troubleshooting

### The deploy job times out or fails

SSH into the server manually and check:
```bash
# Is the deploy user accessible?
ssh -i ~/.ssh/boxtracker_deploy deploy@158.179.31.108 ls -la /opt/boxtracker/

# Is nginx running?
sudo systemctl status nginx

# Is the boxtracker service running?
sudo systemctl status boxtracker
```

### 502 Bad Gateway from Caddy

The .NET service might not be listening. Check:
```bash
sudo journalctl -u boxtracker -n 50
sudo ss -tlnp | grep 5000
```

### HTTPS not working or TLS errors

Check that:
1. Port 443 is open at both the Oracle Cloud console and iptables levels
2. DuckDNS points to the correct IP:
   ```bash
   dig movingstuff.duckdns.org
   ```
3. Caddy is running and has provisioned a cert:
   ```bash
   sudo systemctl status caddy
   sudo journalctl -u caddy -n 50 | grep -i 'tls\|cert'
   ```
4. The Caddyfile is valid:
   ```bash
   sudo caddy validate --config /etc/caddy/Caddyfile
   ```

### Basic auth prompt but credentials don't work

Regenerate the password hash:
```bash
caddy hash-password --plaintext '8busstop'
```

Edit the Caddyfile and replace the hash:
```bash
sudo nano /etc/caddy/Caddyfile
sudo caddy reload --config /etc/caddy/Caddyfile
```

### Database or photos not persisting

Verify `BOXTRACKER_DATA` is set and `/opt/boxtracker/data` is writable:
```bash
sudo ls -la /opt/boxtracker/data/
sudo chown -R deploy:deploy /opt/boxtracker/data
```

---

## Maintenance

### Viewing logs

Backend service:
```bash
sudo journalctl -u boxtracker -f   # follow logs in real-time
```

Caddy (TLS, routing, etc.):
```bash
sudo journalctl -u caddy -f
```

### Manual restart

```bash
sudo systemctl restart boxtracker
sudo caddy reload --config /etc/caddy/Caddyfile
```

### Updating credentials

To change the basic auth password:
```bash
sudo htpasswd -b /etc/nginx/.htpasswd daniel <new-password>
sudo systemctl reload nginx
```

### Backups

Database and photos are automatically backed up hourly to Backblaze B2. See `../lode/infra/backup-system.md` for full details.

**Initial setup** (one-time):
1. Add B2 credentials to GitHub Secrets: `B2_DB_KEY_ID`, `B2_DB_APP_KEY`, `B2_IMAGES_KEY_ID`, `B2_IMAGES_APP_KEY`
2. Allow deploy user to run sudo commands without password (sudoers: `mkdir`, `chown`)
3. Set B2 lifecycle rule on `boxes-db` bucket: delete files older than 30 days

**Verification**:
```bash
tail -f /var/log/boxtracker/backup-db.log
tail -f /var/log/boxtracker/backup-photos.log
```

---

## Architecture Summary

- **Frontend**: Static files from `src/Client/dist/` served by Caddy, with client-side routing fallback to `index.html`
- **Backend**: .NET 10 ASP.NET/Saturn server running on `http://localhost:5000`
- **Caddy**: Reverse proxy and TLS terminator that:
  - Auto-provisions Let's Encrypt certificates via HTTP-01 challenge
  - Applies basic auth to all requests
  - Serves frontend static files for `/`
  - Proxies `/api/` requests to the backend
  - Redirects HTTP → HTTPS automatically
- **Database**: SQLite at `/opt/boxtracker/data/boxtracker.db`
- **Photos**: Uploaded photos stored at `/opt/boxtracker/data/photos/` and served by the backend as static files
- **DNS**: DuckDNS points `movingstuff.duckdns.org` to `158.179.31.108`
