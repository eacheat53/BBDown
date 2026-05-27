using Spectre.Console.Cli;
using System.Threading;

namespace BBDown.Commands;

public class LoginTVCommand : Command<CommandSettings>
{
    protected override int Execute(CommandContext context, CommandSettings settings, CancellationToken cancellationToken)
    {
        BBDownLoginUtil.LoginTV().GetAwaiter().GetResult();
        return 0;
    }
}
