using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Plugin;
using Minecraft.Server.FourKit.Command;
using Minecraft.Server.FourKit.Entity;
using Minecraft.Server.FourKit.Event;
using Minecraft.Server.FourKit.Event.Player;
using Minecraft.Server.FourKit.Event.Block;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class WhitelistPlugin : ServerPlugin
{
    public override string name => "WhitelistPlugin";
    public override string version => "1.8.0";
    public override string author => "Bluer";

    private static string whitelistFile = "plugins/WhitelistPlugin/whitelist.txt";
    private static string codesFile = "plugins/WhitelistPlugin/verified_codes.json";
    private static string ipFile = "plugins/WhitelistPlugin/verified_ips.json";
    private static HashSet<string> whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, (string username, double expires)> codes = new Dictionary<string, (string, double)>();
    private static Dictionary<string, string> verifiedIPs = new Dictionary<string, string>();

    public static void AddVerifiedIP(string name, string ip)
    {
        if (ip != null)
        {
            verifiedIPs[name] = ip;
            var data = new Dictionary<string, string>();
            foreach (var kvp in verifiedIPs)
            {
                data[kvp.Key] = kvp.Value;
            }
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            File.WriteAllText(ipFile, json);
        }
    }

    public static void SaveVerifiedIPs()
    {
        var data = new Dictionary<string, string>();
        foreach (var kvp in verifiedIPs)
        {
            data[kvp.Key] = kvp.Value;
        }
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        File.WriteAllText(ipFile, json);
    }

    private class JoinListener : Listener
    {
        [EventHandler]
        public void onPlayerJoin(PlayerJoinEvent e)
        {
            var player = e.getPlayer();
            if (player == null) return;
            
            string playerName = player.getName();
            if (!IsWhitelisted(playerName))
            {
                player.sendMessage("=== VERIFICATION REQUIRED ===");
                player.sendMessage("You must verify to play!");
                player.sendMessage("Use /verify <code> to verify");
                player.sendMessage("Get your code from Discord!");
                player.sendMessage("==============================");
            }
            else
            {
                try {
                    var addr = player.getAddress();
                    if (addr != null)
                    {
                        string playerIP = addr.ToString();
                        verifiedIPs[playerName] = playerIP;
                        var data = new Dictionary<string, string>();
                        foreach (var kvp in verifiedIPs)
                        {
                            data[kvp.Key] = kvp.Value;
                        }
                        var json = System.Text.Json.JsonSerializer.Serialize(data);
                        File.WriteAllText(ipFile, json);
                    }
                } catch { }
            }
        }
    }

    private class MoveListener : Listener
    {
        [EventHandler]
        public void onPlayerMove(PlayerMoveEvent e)
        {
            var player = e.getPlayer();
            if (player == null) return;
            
            string playerName = player.getName();
            if (!IsWhitelisted(playerName))
            {
                e.setCancelled(true);
                var world = FourKit.getWorld("minecraftoverworld") ?? FourKit.getWorld("world");
                if (world != null)
                {
                    var spawn = world.getSpawnLocation();
                    player.teleport(spawn);
                }
            }
        }
    }

    private class BlockListener : Listener
    {
        [EventHandler]
        public void onBlockBreak(BlockBreakEvent e)
        {
            var player = e.getPlayer();
            if (player == null) return;
            
            string playerName = player.getName();
            if (!IsWhitelisted(playerName))
            {
                e.setCancelled(true);
                player.sendMessage("You must verify first! Use /verify <code>");
            }
        }

        [EventHandler]
        public void onBlockPlace(BlockPlaceEvent e)
        {
            var player = e.getPlayer();
            if (player == null) return;
            
            string playerName = player.getName();
            if (!IsWhitelisted(playerName))
            {
                e.setCancelled(true);
                player.sendMessage("You must verify first! Use /verify <code>");
            }
        }
    }

    public override void onEnable()
    {
        Directory.CreateDirectory("plugins/WhitelistPlugin");
        LoadWhitelist();
        LoadCodes();
        LoadIPs();

        FourKit.addListener(new JoinListener());
        FourKit.addListener(new MoveListener());
        FourKit.addListener(new BlockListener());
        FourKit.getCommand("verify").setExecutor(new VerifyCommand());
        FourKit.getCommand("saveip").setExecutor(new SaveIPCommand());
    }

    public override void onDisable() { }

    private void LoadWhitelist()
    {
        string ownerUid = "c8b4bcfb-faff-8a66-7f1e-c159747a5b4f";
        
        if (!File.Exists(whitelistFile))
        {
            string[] defaults = { ownerUid };
            File.WriteAllLines(whitelistFile, defaults);
            whitelist = new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var lines = File.ReadAllLines(whitelistFile);
            whitelist = new HashSet<string>(lines.Select(w => w.Trim()).Where(w => !string.IsNullOrEmpty(w)), StringComparer.OrdinalIgnoreCase);
            if (!whitelist.Contains(ownerUid))
            {
                File.AppendAllLines(whitelistFile, new[] { ownerUid });
                whitelist.Add(ownerUid);
            }
        }
    }

    private static void LoadCodes()
    {
        codes.Clear();
        if (!File.Exists(codesFile)) return;
        try
        {
            var json = File.ReadAllText(codesFile);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, CodeData>>(json);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    codes[kvp.Key] = (kvp.Value.username, kvp.Value.expires);
                }
            }
        }
        catch { }
    }

    private static void LoadIPs()
    {
        verifiedIPs.Clear();
        if (!File.Exists(ipFile)) return;
        try
        {
            var json = File.ReadAllText(ipFile);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    verifiedIPs[kvp.Key] = kvp.Value;
                }
            }
        }
        catch { }
    }

    public static void ReloadWhitelist()
    {
        if (File.Exists(whitelistFile))
        {
            var lines = File.ReadAllLines(whitelistFile);
            whitelist = new HashSet<string>(lines.Select(w => w.Trim()).Where(w => !string.IsNullOrEmpty(w)), StringComparer.OrdinalIgnoreCase);
        }
    }

    public static bool IsWhitelisted(string name)
    {
        return whitelist.Contains(name);
    }

    public static bool VerifyCode(string code, string playerName, string playerIP)
    {
        LoadCodes();
        
        if (codes.TryGetValue(code, out var data))
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < data.expires)
            {
                if (data.username.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    whitelist.Add(playerName);
                    File.AppendAllLines(whitelistFile, new[] { playerName });
                    codes.Remove(code);
                    SaveCodes();
                    ReloadWhitelist();
                    
                    if (playerIP != null)
                    {
                        AddVerifiedIP(playerName, playerIP);
                    }
                    
                    return true;
                }
            }
        }
        return false;
    }

    private static void SaveCodes()
    {
        var data = new Dictionary<string, CodeData>();
        foreach (var kvp in codes)
        {
            data[kvp.Key] = new CodeData { username = kvp.Value.username, expires = kvp.Value.expires };
        }
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        File.WriteAllText(codesFile, json);
    }

    private class CodeData
    {
        public string username { get; set; }
        public double expires { get; set; }
    }
}

public class VerifyCommand : CommandExecutor
{
    public bool onCommand(CommandSender sender, Command command, string label, string[] args)
    {
        if (sender is not Player player) return true;

        if (args.Length < 1)
        {
            player.sendMessage("Usage: /verify <code>");
            return true;
        }

        string code = args[0];
        string playerName = player.getName();
        string playerIP = player.getAddress()?.ToString();

        if (WhitelistPlugin.VerifyCode(code, playerName, playerIP))
        {
            player.sendMessage("You are now verified!");
            player.sendMessage("Welcome to Blue SMP!");
        }
        else
        {
            player.sendMessage("Invalid or expired code.");
        }
        return true;
    }
}

public class SaveIPCommand : CommandExecutor
{
    public bool onCommand(CommandSender sender, Command command, string label, string[] args)
    {
        if (sender is not Player player) return true;
        
        string adminUid = player.getUniqueId().ToString();
        if (adminUid != "c8b4bcfb-faff-8a66-7f1e-c159747a5b4f")
        {
            player.sendMessage("Only owner can use this.");
            return true;
        }
        
        var world = FourKit.getWorld("minecraftoverworld") ?? FourKit.getWorld("world");
        if (world == null) return true;
        
        var players = world.getPlayers();
        if (players == null) return true;
        
        int saved = 0;
        foreach (var p in players)
        {
            string name = p.getName();
            string ip = p.getAddress()?.ToString();
            if (ip != null)
            {
                WhitelistPlugin.AddVerifiedIP(name, ip);
                saved++;
            }
        }
        
        player.sendMessage("Saved IPs for " + saved + " players!");
        return true;
    }
}
