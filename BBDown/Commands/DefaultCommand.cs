using Spectre.Console.Cli;
using System.Threading;

namespace BBDown.Commands;

public class DefaultCommand : Command<MyOption>
{
    protected override int Execute(CommandContext context, MyOption settings, CancellationToken cancellationToken)
    {
        _ = BBDownUtil.CheckUpdateAsync();
        Program.DoWorkAsync(settings).GetAwaiter().GetResult();
        return 0;
    }
}
