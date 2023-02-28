using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace bot;

public class SlashModules{
    
    public class HelpCommand : InteractionModuleBase<SocketInteractionContext>{
        // TODO
        /// <summary>
        /// Instead of listing and describing every fucking single command by hand,
        /// I should try getting all commands from the Interaction Module ¯\_(ツ)_/¯
        /// </summary>
        [SlashCommand("help", "")]
        public async Task Help(){
            await RespondAsync("todo");
        }
    }

    public class RandomImageModule : InteractionModuleBase<SocketInteractionContext>{
        
        [SlashCommand("image", "ein random image aus einem channel")]
        public async Task RandomImageK([Discord.Commands.Summary("The name of the channel")] string channelName){
            // Get the channel by name
            var channel = Context.Guild.Channels.FirstOrDefault(x => x.Name == channelName) as SocketTextChannel;

            if (channel == null){
                await ReplyAsync("Falscher Channel Name.");
                return;
            }

            // Get all the messages from the channel
            var messages = await channel.GetMessagesAsync().FlattenAsync();

            // Filter  messages that are actually images (attachments)
            var images = messages.Where(x => x.Attachments.Count > 0).ToList();

            if (images.Count == 0){
                await ReplyAsync("Keine Bilder im Channel.");
                return;
            }

            // just learnt this today xD
            var random = new Random();
            var index = random.Next(images.Count);
            
            // Send the image as a file 
            var image = images[index];
            var url = image.Attachments.First().Url;
            var stream = await DownloadImageAsync(url);
            await Context.Channel.SendFileAsync(stream, "random_image.jpg");
        }

        /// <summary>
        /// The downloaded images from the helper function are stored in memory as streams,
        /// not as files on disk. Therefore, they do not need to be deleted manually after the
        /// image was successfully sent.
        /// TODO: dispose the stream after using it to free up some memory resources.
        /// </summary>
        /// <param name="url">Discord's own URL for storing files</param>
        /// <returns></returns>
        private async Task<Stream> DownloadImageAsync(string url){
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
