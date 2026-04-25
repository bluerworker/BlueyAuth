# BlueyAuth

Discord verification bot for FourKit Minecraft servers.

## Features

- Players verify via Discord channel
- DM or channel code delivery
- One-time codes (10 min expiry)

## Setup

1. Install Python:
```
pip install discord.py
```

2. Edit `verify_bot.py`:
```python
channel_name = "your-channel-name"  # Discord channel
codes_path = r"C:\Your\Server\path\verified_codes.json"
BOT_TOKEN = "your-bot-token"
```

3. Run:
```
python verify_bot.py
```

## Usage

1. Player types Minecraft name in #verify channel
2. Bot sends code (DM)
3. Player joins game and runs `/verify <code>`
4. If valid, they're whitelisted

## Commands

- In Discord: Type Minecraft username in channel
- In Game: `/verify <code>`

## WhitelistPlugin

Get `WhitelistPlugin.cs` from your server plugins folder.

## Credits

Created by Bluerworker!
