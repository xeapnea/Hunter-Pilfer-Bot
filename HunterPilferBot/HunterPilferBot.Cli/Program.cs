using System;
using System.Threading.Tasks;

namespace HunterPilferBot.Cli
{
	using Commands.Example;
	using Core;

	public class Program
	{
		public static async Task Main(string[] args)
		{
			var worked = await Runner.Start()
				.Services(_ =>
				{

				})
				.Config("appsettings.json")
				.Get<IPilferBot>()
				.Module<ExampleCommand>()
				.Start();

			if (!worked)
			{
				Console.WriteLine("Failed to login!");
				return;
			}

			Console.WriteLine("Login success!");
			while(Console.ReadKey().Key != ConsoleKey.E)
			{
				Console.WriteLine("Press \"E\" to exit...");
			}
		}
	}
}
