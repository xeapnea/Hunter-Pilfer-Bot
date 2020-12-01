using Discord.Commands;
using System.Threading.Tasks;

namespace HunterPilferBot.Commands.Example
{
	[Name("Example Commands")]
	public class ExampleCommand : ModuleBase<SocketCommandContext>
	{
		[Command("test", RunMode = RunMode.Async)]
		[Summary("Checks to see if the bot is active")]
		public async Task Test([Remainder]string message = null)
		{
			await ReplyAsync($"Hello world! I see your \"{message}\", {Context.User.Username}");
		}
	}
}
