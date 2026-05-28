using Spectre.Console.Cli;
using System.Threading;
using System.Threading.Tasks;

namespace BBDown.Commands;

public class DefaultCommand : AsyncCommand<MyOption>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, MyOption settings, CancellationToken cancellationToken)
    {
        _ = BBDownUtil.CheckUpdateAsync();
        await Program.DoWorkAsync(settings);
        return 0;
    }
}
