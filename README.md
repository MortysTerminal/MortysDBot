# MortysDBot

Discord Bot in C#/.NET.

## Local Run
- Set Discord token via user-secrets:
  - `dotnet user-secrets set "Discord:Token" "..."` (in src/MortysDBot.Bot)
- Run:
  - `dotnet run --project src/MortysDBot.Bot`

## Docker (Proxmox)
See `docker/`.
