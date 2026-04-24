import discord
import random
import json
import os
import time
import re

intents = discord.Intents.default()
intents.message_content = True

client = discord.Client(intents=intents)

# === CONFIG ===
channel_name = "username-verify"  # Discord channel name
codes_path = r"CHANGE_THIS\plugins\WhitelistPlugin\verified_codes.json"  # Must match server path
# ===========

codes = {}

def clean_name(text):
    text = re.sub(r'<:[a-zA-Z0-9_]+:[0-9]+>', '', text)
    text = re.sub(r'<a:[a-zA-Z0-9_]+:[0-9]+>', '', text)
    emoji_pattern = re.compile("["
        u"\U0001F600-\U0001F64F"
        u"\U0001F300-\U0001F5FF"
        u"\U0001F680-\U0001F6FF"
        u"\U0001F1E0-\U0001F1FF"
        u"\U00002702-\U000027B0"
        u"\U000024C2-\U0001F251"
        "]+", flags=re.UNICODE)
    text = emoji_pattern.sub('', text)
    text = re.sub(r'[^a-z0-9_]', '', text.lower())
    return text

@client.event
async def on_ready():
    print(f"Bot ready: {client.user}")
    load_codes()

@client.event
async def on_message(message):
    if message.author.bot:
        return
    
    try:
        if message.channel.name.lower() != channel_name.lower():
            return
    except:
        return
    
    username = message.content.strip()
    username = clean_name(username)
    
    if not username or len(username) < 2:
        return
    
    code = str(random.randint(100000, 999999))
    codes[code] = {"username": username, "expires": int(time.time()) + 600}
    save_codes()
    
    try:
        dm = await message.author.create_dm()
        await dm.send(f"Your code: {code} - use /verify {code} in game")
        await message.reply("Check your DMs!")
    except:
        await message.reply(f"Your code: {code}")

def load_codes():
    global codes
    if os.path.exists(codes_path):
        with open(codes_path, "r") as f:
            codes = json.load(f)

def save_codes():
    os.makedirs(os.path.dirname(codes_path), exist_ok=True)
    with open(codes_path, "w") as f:
        json.dump(codes, f)

# YOUR BOT TOKEN - Get from https://discord.com/developers/applications
BOT_TOKEN = "YOUR_TOKEN_HERE"

client.run(BOT_TOKEN)