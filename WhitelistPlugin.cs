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
    public override string version => "1.5.0";
    public override string author => "Bluer";

    private static string whitelistFile = "plugins/WhitelistPlugin/whitelist.txt";
    private static string codesFile = @"CHANGE_PATH_TO_VERIFIED_CODES_JSON";
    private static HashSet<string> whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, (string username, double expires)> codes = new Dictionary<string, (string, double)>();

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

        FourKit.addListener(new JoinListener());
        FourKit.addListener(new MoveListener());
        FourKit.addListener(new BlockListener());
        FourKit.getCommand("verify").setExecutor(new VerifyCommand());
    }

    public override void onDisable() { }

    private void LoadWhitelist()
    {
        if (!File.Exists(whitelistFile))
        {
            string[] defaults = { "c8b4bcfb-faff-8a66-7f1e-c159747a5b4f" };
            File.WriteAllLines(whitelistFile, defaults);
            whitelist = new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var lines = File.ReadAllLines(whitelistFile);
            whitelist = new HashSet<string>(lines.Select(w => w.Trim()).Where(w => !string.IsNullOrEmpty(w)), StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void LoadCodes()
    {
        codes.Clear();
        if (!File.Exists(codesFile)) 
        {
            Console.WriteLine("Codes file not found");
            return;
        }
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
            Console.WriteLine("Loaded " + codes.Count + " codes");
        }
        catch (Exception e) { Console.WriteLine("LoadCodes error: " + e.Message); }
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

    public static bool VerifyCode(string code, string playerName)
    {
        LoadCodes();
        
        if (codes.TryGetValue(code, out var data))
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Console.WriteLine("Code " + code + " expires: " + data.expires + " now: " + now);
            if (now < data.expires)
            {
                if (data.username.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    whitelist.Add(playerName);
                    File.AppendAllLines(whitelistFile, new[] { playerName });
                    codes.Remove(code);
                    SaveCodes();
                    ReloadWhitelist();
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

        if (WhitelistPlugin.VerifyCode(code, playerName))
        {
            player.sendMessage("You are now verified!");
            player.sendMessage("Welcome");
        }
        else
        {
            player.sendMessage("Invalid or expired code.");
        }
        return true;
    }
}
