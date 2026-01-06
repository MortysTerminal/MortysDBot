# MortysDBot Docker Deployment

## Requirements
- Docker + Docker Compose Plugin

## Configuration
Create environment variables in Portainer Stack (recommended) or via `.env`:
- `DISCORD_TOKEN` (required)
- `DISCORD_GUILD_ID` (optional, speeds up slash command registration)

## Deploy / Update
- Portainer: Stack -> Update the stack -> Pull latest & redeploy
- CLI (if used):
  - `docker compose up -d --build`

## Logs
- Portainer: Container -> Logs
- CLI:
  - `docker compose logs -f bot`

## Restart
- Portainer: restart container
- CLI:
  - `docker compose restart bot`
