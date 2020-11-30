using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace HunterPilferBot.Core
{
	public interface IRunner
	{
		IRunner Services(Action<IServiceCollection> action);
		IRunner Services(Action<IServiceCollection, IConfiguration> action);
		IRunner Config(Action<IConfigurationBuilder> action);
		IRunner Config(params string[] files);
		IServiceProvider Provider();
		T Get<T>();
	}

	public class Runner : IRunner
	{
		private readonly List<Action> _actions;
		private readonly IConfigurationBuilder _configBuilder;
		private readonly IServiceCollection _services;

		private IConfiguration _config;
		private ServiceProvider _provider;

		public Runner()
		{
			_actions = new List<Action>();
			_configBuilder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.AddCommandLine(Environment.GetCommandLineArgs());
			_services = new ServiceCollection();
		}

		public IRunner Services(Action<IServiceCollection> action)
		{
			_actions.Add(() => action(_services));
			return this;
		}

		public IRunner Services(Action<IServiceCollection, IConfiguration> action)
		{
			_actions.Add(() => action(_services, _config));
			return this;
		}

		public IRunner Config(Action<IConfigurationBuilder> action)
		{
			action(_configBuilder);
			return this;
		}

		public IRunner Config(params string[] files)
		{
			return Config(c =>
			{
				foreach (var file in files)
				{
					var ext = Path.GetExtension(file).Trim('.').ToLower();

					switch (ext)
					{
						case "xml": c.AddXmlFile(file, true, true); break;
						case "json": c.AddJsonFile(file, true, true); break;
						case "ini": c.AddIniFile(file, true, true); break;
						default: throw new Exception($"Given config file type is not supported: {ext} - {file}");
					}
				}
			});
		}

		public IServiceProvider Provider()
		{
			if (_provider != null)
				return _provider;

			_config = _configBuilder.Build();

			foreach (var action in _actions)
				action();

			_provider = _services
				.AddSingleton(_config)
				.AddTransient<IPilferBot, PilferBot>()
				.AddLogging(_ =>
				{
					_.AddSerilog(new LoggerConfiguration()
					 .MinimumLevel.Debug()
					 .Enrich.FromLogContext()
					 .WriteTo.Console()
					 .WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day)
					 .CreateLogger());
				})
				.BuildServiceProvider();

			_services.AddSingleton<IServiceProvider>(_provider);

			return _provider;
		}

		public T Get<T>()
		{
			return Provider().GetRequiredService<T>();
		}

		public static IRunner Start()
		{
			return new Runner();
		}
	}
}
