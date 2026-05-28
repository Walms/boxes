# BoxTracker Server Setup Guide

This guide walks through setting up the Oracle VM (158.179.31.108) to run BoxTracker with auto-deployments from GitHub Actions.

## Prerequisites
- SSH access to the server as the `ubuntu` user
- A GitHub repository set up with this codebase
- An SSH ed25519 keypair for deployment (see step 3)

---

## Step 1: Install .NET 10 Runtime and nginx

SSH into the server and run:

```bash
# Install .NET 10 runtime
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime dotnet --version 10.0.0 --install-dir /opt/dotnet
echo 'export PATH=$PATH:/opt/dotnet' | sudo tee /etc/profile.d/dotnet.sh
source /etc/profile.d/dotnet.sh

# Install nginx
sudo apt update && sudo apt install -y nginx
```

Verify the installation:
```bash
/opt/dotnet/dotnet --version
nginx -v
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

## Step 6: Set Up nginx

### Create the .htpasswd file (basic auth)

```bash
sudo apt install -y apache2-utils
sudo htpasswd -cb /etc/nginx/.htpasswd daniel 8busstop
sudo chmod 640 /etc/nginx/.htpasswd
sudo chown root:www-data /etc/nginx/.htpasswd
```

### Configure the nginx site

Copy the `nginx.conf` file from this repo (or create `/etc/nginx/sites-available/boxtracker` with its contents):

```bash
sudo cp nginx.conf /etc/nginx/sites-available/boxtracker
# OR manually edit:
# sudo nano /etc/nginx/sites-available/boxtracker
```

Enable the site:
```bash
sudo ln -s /etc/nginx/sites-available/boxtracker /etc/nginx/sites-enabled/
```

Test and reload:
```bash
sudo nginx -t
# Should output: test is successful
sudo systemctl reload nginx
```

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

Visit `http://158.179.31.108` in your browser:
- You should be prompted for a username/password
- Login with: `daniel` / `8busstop`
- The BoxTracker frontend should load
- Navigate around and verify API calls work (check browser console for errors)

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

### 502 Bad Gateway from nginx

The .NET service might not be listening. Check:
```bash
sudo journalctl -u boxtracker -n 50
sudo netstat -tulpn | grep 5000
```

### Basic auth prompt but credentials don't work

Regenerate the htpasswd file:
```bash
sudo htpasswd -b /etc/nginx/.htpasswd daniel 8busstop
sudo systemctl reload nginx
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

nginx:
```bash
sudo tail -f /var/log/nginx/error.log
```

### Manual restart

```bash
sudo systemctl restart boxtracker
sudo systemctl reload nginx
```

### Updating credentials

To change the basic auth password:
```bash
sudo htpasswd -b /etc/nginx/.htpasswd daniel <new-password>
sudo systemctl reload nginx
```

---

## Architecture Summary

- **Frontend**: Static files from `src/Client/dist/` served by nginx, with client-side routing fallback to `index.html`
- **Backend**: .NET 10 ASP.NET/Saturn server running on `http://localhost:5000`
- **nginx**: Reverse proxy that:
  - Applies basic auth to all requests
  - Serves frontend static files for `/`
  - Proxies `/api/` requests to the backend
- **Database**: SQLite at `/opt/boxtracker/data/boxtracker.db`
- **Photos**: Uploaded photos stored at `/opt/boxtracker/data/photos/` and served by the backend as static files
