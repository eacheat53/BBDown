using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading;

namespace BBDown.Commands;

public class ServeSettings : CommandSettings
{
    [CommandOption("-l|--listen")]
    [Description("服务器监听url")]
    public string ListenUrl { get; set; } = "http://0.0.0.0:23333";
}

public class ServeCommand : Command<ServeSettings>
{
    protected override int Execute(CommandContext context, ServeSettings settings, CancellationToken cancellationToken)
    {
        _ = BBDownUtil.CheckUpdateAsync();
        Program.StartServer(settings.ListenUrl);
        return 0;
    }
}
