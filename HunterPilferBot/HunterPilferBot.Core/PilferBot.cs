using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HunterPilferBot.Core
{
	public interface IPilferBot
	{
		IPilferBot Module(Type type);
		IPilferBot Module<T>();
		IPilferBot Module(Assembly assembly);
		Task<bool> Start();
	}

	public class PilferBot : IPilferBot
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		private readonly List<Assembly> _assemblies;
		private readonly List<Type> _modules;

		public string Prefix => _config["Account:Prefix"] ?? "!";
		public string Token => _config["Account:Token"] ?? throw new ArgumentNullException("Account token");

		public DiscordSocketClient Client { get; private set; }
		public CommandService Commands { get; private set; }

		public PilferBot(IServiceProvider serviceProvider, ILogger<PilferBot> logger, IConfiguration config)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
			_config = config;

			_assemblies = new List<Assembly>();
			_modules = new List<Type>();
		}

		public IPilferBot Module(Type type)
		{
			_modules.Add(type);
			return this;
		}

		public IPilferBot Module<T>() => Module(typeof(T));

		public IPilferBot Module(Assembly assembly)
		{
			_assemblies.Add(assembly);
			return this;
		}

		public async Task<bool> Start()
		{
			try
			{
				_logger.LogInformation($"Starting discord bot");
				Client = new DiscordSocketClient(new DiscordSocketConfig{ LogLevel = LogSeverity.Debug });
				Commands = new CommandService(new CommandServiceConfig{ LogLevel = LogSeverity.Debug });

				Hook(Client, Commands);

				await Client.LoginAsync(TokenType.Bot, Token);
				await Client.StartAsync();

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while attempting to login...");
				return false;
			}
		}

		private void Hook(DiscordSocketClient client, CommandService commands)
		{
			client.Log += Client_Log;
			commands.Log += Client_Log;

			client.Disconnected += Client_Disconnected;
			client.Connected += Client_Connected;

			client.LoggedIn += Client_LoggedIn;
			client.LoggedOut += Client_LoggedOut;

			client.MessageReceived += (a) => Client_MessageReceived(client, commands, a);
		}

		private async Task Client_MessageReceived(DiscordSocketClient client, CommandService commands, SocketMessage arg)
		{
			int argPos = 0;
			if (arg.Author.IsBot ||
				!(arg is SocketUserMessage msg) ||
				(!msg.HasStringPrefix(Prefix, ref argPos) &&
				!msg.HasMentionPrefix(client.CurrentUser, ref argPos)))
				return;

			var context = new SocketCommandContext(client, msg);

			using (context.Channel.EnterTypingState())
			{
				try
				{
					var result = await commands.ExecuteAsync(context, argPos, _serviceProvider);
					if (result.IsSuccess)
						return;

					_logger.LogError($"Client::MessageReceived >> Error Occurred: {result.ErrorReason} at {context.Guild.Name}");
					switch (result.Error)
					{
						case CommandError.UnknownCommand: break;
						case CommandError.BadArgCount:
							await context.Channel.SendMessageAsync($"Invalid use of this command. Please type `{Prefix} help [command name]` for help!");
							break;
						case CommandError.UnmetPrecondition:
							await context.Channel.SendMessageAsync($"You can not use this command at the moment.\r\nReason: " + result.ErrorReason);
							break;
						default:
							await context.Channel.SendMessageAsync($"An error occurred while attempting to run your command: {result.Error}");
							break;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error occurred while trying to execute command: {context.Channel.Name}: {context.Message.Content}");
				}
			}

		}

		private Task Client_LoggedOut()
		{
			_logger.LogInformation("Client:LoggedOut >> Logged out of discord");
			return Task.CompletedTask;
		}

		private Task Client_LoggedIn()
		{
			_logger.LogInformation("Client::LoggedIn >> Logged into discord");
			return Task.CompletedTask;
		}

		private Task Client_Connected()
		{
			_logger.LogInformation("Client::Connected >> Connected to discord");
			return Task.CompletedTask;
		}

		private Task Client_Disconnected(Exception arg)
		{
			_logger.LogError(arg, "Client::Disconnected >> Disconnected from discord.");
			return Task.CompletedTask;
		}

		private Task Client_Log(LogMessage arg)
		{
			var level = arg.Severity.TransformLogLevel();
			_logger.Log(level, arg.Exception, $"Client::Log >> {arg.Source}: {arg.Message}");
			return Task.CompletedTask;
		}
	}
}
