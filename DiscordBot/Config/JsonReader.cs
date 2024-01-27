using Newtonsoft.Json;

namespace DiscordBot.config
{
    public class JsonReader
    {
        public string Token { get; set; }
        public string Prefix { get; set; }

        public async Task ReadJsonAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync("config.json").ConfigureAwait(false);
                var jsonStructure = JsonConvert.DeserializeObject<JsonStructure>(json);

                if (jsonStructure != null && !string.IsNullOrEmpty(jsonStructure.Token) && !string.IsNullOrEmpty(jsonStructure.Prefix))
                {
                    Token = jsonStructure.Token;
                    Prefix = jsonStructure.Prefix;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File 'config.json' not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading the json file. Exception: {ex.Message}");
            }
        }
    }

    internal sealed record JsonStructure(string Token, string Prefix);
}