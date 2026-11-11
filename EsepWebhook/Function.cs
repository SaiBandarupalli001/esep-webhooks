using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogLine($"Received: {input}");

        var json = JToken.Parse(input?.ToString() ?? "{}");
        string? issueUrl = json.Type == JTokenType.Object
            ? json["issue"]?["html_url"]?.ToString()
            : null;

        if (string.IsNullOrEmpty(issueUrl))
            return "ignored";

        string? slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");

        if (string.IsNullOrEmpty(slackUrl))
            throw new Exception("SLACK_URL is missing");

        string payload = JsonConvert.SerializeObject(new
        {
            text = $"Issue Created: {issueUrl}"
        });

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        using var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
