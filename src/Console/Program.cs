using System.Runtime.Serialization;
using System.Text.Json;
using RestSharp;
using RestSharp.Serializers.Json;

namespace ChatGPT;

public static class StringUtils
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }
}

class Program
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

        public override string ConvertName(string name)
        {
            return name.ToSnakeCase();
        }
    }

    internal class Message
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";

        // necessary for System.Text.Json to deserialize responses
        public Message()
        {
        }

        public Message(string content)
        {
            Content = content;
        }

        public Message(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    internal class ChatGPTCompletionRequest
    {
        public string Model { get; set; } = "gpt-3.5-turbo";
        public IList<Message> Messages { get; set; } = new List<Message>();
    }

    internal class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; } = new Message("");
        public string FinishReason { get; set; } = "";
    }

    internal class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    internal class ChatGPTCompletionResponse
    {
        public string Id { get; set; } = "";
        public string Object { get; set; } = "";
        public int Created { get; set; }
        public string Model { get; set; } = "";
        public Usage Usage { get; set; } = new Usage();
        public IList<Choice> Choices { get; set; } = new List<Choice>();
    }

    public const string OpenApiUrl = "https://api.openai.com";
    private static readonly IRestClient client = new RestClient(
        OpenApiUrl,
        configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
        })
    );

    static async Task Main(string[] args)
    {
        Console.Write("Enter your question for ChatGPT: ");
        var question = Console.ReadLine();
        if (string.IsNullOrEmpty(question))
        {
            return;
        }

        var answer = await PromptChatGPT(question);

        Console.WriteLine();
        Console.WriteLine(answer);
    }

    public static async Task<string> PromptChatGPT(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return "Please set $OPENAI_API_KEY";
        }

        var request = new RestRequest("/v1/chat/completions");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", "Bearer " + apiKey);

        var data = new ChatGPTCompletionRequest();
        data.Messages.Add(new Message("system", "You are a helpful assistant."));
        data.Messages.Add(new Message(prompt));

        request.AddJsonBody(data);

        var response = await client.PostAsync<ChatGPTCompletionResponse>(request);

        if (response is null)
        {
            return "Failed to prompt ChatGPT";
        }

        return response.Choices.First().Message.Content.Trim();
    }
}
