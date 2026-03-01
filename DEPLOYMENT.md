# BongoCat Server Deployment Guide

## Manual Deployment with Docker Compose

```bash
# Start the server
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the server
docker-compose down

# Update and restart
git pull
docker-compose up -d --build
```

## Auto-start with systemd

### Installation Steps

1. **Edit the service file** with your settings:
```bash
# Open bongocat-server.service and replace:
# - YOUR_USERNAME with your actual username
# - /opt/BongoCatServer with your actual project path (if different)
```

2. **Copy the service file to systemd:**
```bash
sudo cp bongocat-server.service /etc/systemd/system/
```

3. **Enable and start the service:**
```bash
sudo systemctl daemon-reload
sudo systemctl enable bongocat-server
sudo systemctl start bongocat-server
```

### Service Management Commands

```bash
# Check status
sudo systemctl status bongocat-server

# View logs
sudo journalctl -u bongocat-server -f

# Restart
sudo systemctl restart bongocat-server

# Stop
sudo systemctl stop bongocat-server

# Disable auto-start
sudo systemctl disable bongocat-server
```

### After Code Updates

```bash
cd /opt/BongoCatServer
git pull
sudo systemctl restart bongocat-server
```

## Network Configuration

### Ports
- **2017**: TCP port for incoming key events
- **2018**: WebSocket port for clients

### Local Network Access
Clients can connect to `<server-ip>:2017` and `<server-ip>:2018`

### Internet Access
Configure port forwarding on your router:
- Forward external port 2017 → internal IP:2017
- Forward external port 2018 → internal IP:2018

## Firewall Configuration

If using UFW:
```bash
sudo ufw allow 2017/tcp
sudo ufw allow 2018/tcp
```

If using firewalld:
```bash
sudo firewall-cmd --permanent --add-port=2017/tcp
sudo firewall-cmd --permanent --add-port=2018/tcp
sudo firewall-cmd --reload
```
