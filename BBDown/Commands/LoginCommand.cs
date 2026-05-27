using Spectre.Console.Cli;
using System.Threading;

namespace BBDown.Commands;

public class LoginCommand : Command<CommandSettings>
{
    protected override int Execute(CommandContext context, CommandSettings settings, CancellationToken cancellationToken)
    {
        BBDownLoginUtil.LoginWEB().GetAwaiter().GetResult();
        return 0;
    }
}
