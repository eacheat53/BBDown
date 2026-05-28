using System;
using BBDown.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Spectre.Console.Cli;

namespace BBDown;

internal static class BBDownConfigParser
{
    public static List<string> MergeWithConfig(string[] cliArgs)
    {
        var result = new List<string>(cliArgs);

        var configPath = cliArgs.Contains("--config-file")
            ? cliArgs.ElementAtOrDefault(Array.IndexOf(cliArgs, "--config-file") + 1)
            : null;

        if (string.IsNullOrEmpty(configPath))
            configPath = Path.Combine(Program.APP_DIR, "BBDown.config");

        if (!File.Exists(configPath))
            return result;

        Logger.Log($"加载配置文件: {configPath}");

        var configArgs = File.ReadAllLines(configPath)
            .Where(s => !string.IsNullOrWhiteSpace(s) && !s.TrimStart().StartsWith('#'))
            .SelectMany(line =>
            {
                var trim = line.Trim();
                if (trim.StartsWith('-') && trim.Contains(' '))
                {
                    var idx = trim.IndexOf(' ');
                    return new[] { trim[..idx], trim[idx..].Trim().Trim('"') };
                }
                return new[] { trim.Trim('"') };
            })
            .ToList();

        var aliasMap = BuildAliasMap();

        var explicitOptions = new HashSet<string>();
        for (int i = 0; i < cliArgs.Length; i++)
        {
            if (cliArgs[i].StartsWith('-') && aliasMap.TryGetValue(cliArgs[i], out var canonical))
            {
                explicitOptions.Add(canonical);
            }
        }

        for (int i = 0; i < configArgs.Count;)
        {
            var name = configArgs[i];
            if (!name.StartsWith('-'))
            {
                result.Add(name);
                i++;
                continue;
            }

            if (aliasMap.TryGetValue(name, out var canonical))
            {
                if (!explicitOptions.Contains(canonical))
                {
                    result.Add(name);
                    i++;
                    while (i < configArgs.Count && !configArgs[i].StartsWith('-'))
                    {
                        result.Add(configArgs[i]);
                        i++;
                    }
                }
                else
                {
                    i++;
                    while (i < configArgs.Count && !configArgs[i].StartsWith('-')) i++;
                }
            }
            else
            {
                result.Add(name);
                i++;
            }
        }

        Logger.LogDebug("新的命令行参数: " + string.Join(" ", result));
        return result;
    }

    private static Dictionary<string, string> BuildAliasMap()
    {
        var map = new Dictionary<string, string>();

        void ScanType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<CommandOptionAttribute>();
                if (attr != null)
                {
                    var canonical = prop.Name;
                    foreach (var name in attr.LongNames)
                    {
                        map["--" + name] = canonical;
                    }
                    foreach (var name in attr.ShortNames)
                    {
                        map["-" + name] = canonical;
                    }
                }
            }
        }

        ScanType(typeof(MyOption));
        ScanType(typeof(Commands.ServeSettings));
        return map;
    }
}
