using System.Reflection;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Discord;

/// <summary>
/// tinkering with discord.NET Interactions.
/// just a playground 
/// </summary>

public class program
{
    
    private static DiscordSocketClient? discordClient;

    // Calls async method because fuck Main for not being async
    public static Task Main(string[] args) => new program().MainAsync();

    async Task MainAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => services
                .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = Discord.GatewayIntents.AllUnprivileged,
                    AlwaysDownloadUsers = true,
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>())
                .UseConsoleLifetime()
            .Build();
        
        await host.StartAsync();
        await RunAsync(host);
    }

    private async Task RunAsync(IHost host)
    {
        using IServiceScope serviceScope = host.Services.CreateScope();
        IServiceProvider provider = serviceScope.ServiceProvider;
        
        discordClient = provider.GetRequiredService<DiscordSocketClient>();
        var interactionService = provider.GetRequiredService<InteractionService>();
        
        await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

        discordClient.Ready += async () =>
        {
            await interactionService.RegisterCommandsGloballyAsync();
        };

        // Get the token from the environment variable (thanks for scanning for credentials GitHub <3)
        var TOKEN = Environment.GetEnvironmentVariable("discordToken");
        await discordClient.LoginAsync(Discord.TokenType.Bot, TOKEN);
        
        await discordClient.StartAsync();
        await Task.Delay(-1);
    }
    
    public class InteractionHandler
    {
        DiscordSocketClient client;
        InteractionService interactionService;
        IServiceProvider serviceProvider;

        // Using constructor injection
        public InteractionHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider serviceProvider)
        {
            this.client = client;
            this.interactionService = interactionService;
            this.serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            client.InteractionCreated += HandleSocketInteraction;
            
            interactionService.SlashCommandExecuted += (info, context, result) => 
                { Console.WriteLine($"Slash command {info.Name} executed"); return Task.CompletedTask; };
        }

        /// <summary>
        /// This function handles an interaction from a socket (such as a slash command)
        /// </summary>
        /// <param name="interaction"></param>
        private async Task HandleSocketInteraction(SocketInteraction interaction) {
            // Try to execute the interaction as a command
            try {
                // Create a context object for the interaction
                var context = new SocketInteractionContext(client, interaction);
                // Execute the command using the interaction service and the service provider
                await interactionService.ExecuteCommandAsync(context, serviceProvider);
            }
            // Catch any exceptions that may occur
            catch (Exception error) {
                // If the interaction was an application command (slash command)
                if (interaction.Type == InteractionType.ApplicationCommand) {
                    // Delete the original response message from the bot
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (message) => await message.Result.DeleteAsync());
                }
            }
        }
    }
}