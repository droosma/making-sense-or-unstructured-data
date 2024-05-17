using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class ChatFileParser
{
    private readonly IChatCompletionService _completionService;
    private readonly Kernel _kernel;

    public ChatFileParser(IKernelBuilder kernelBuilder)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
                                                 {
                                                     builder.AddConsole();
                                                     builder.SetMinimumLevel(LogLevel.Trace);
                                                 });
        kernelBuilder.Services.AddSingleton(loggerFactory);
        kernelBuilder.Plugins.AddFromType<CurrencyPlugin>();
        
        _kernel = kernelBuilder.Build();
        _completionService = _kernel.GetRequiredService<IChatCompletionService>("gpt-35");
    }

    public async Task<Listing> ExtractListing(string listingText)
    {
        var history = new ChatHistory
                      {
                          new(AuthorRole.System,
                              """
                              tasked with converting a single car listing into a sting structured JSON format

                              ### Tasks:

                              1. Ensure no information is omitted. Include all text as it appears in the file.
                              2. Produce a single valid JSON representation of the object. 
                              3. The output must be directly parsable into an Listing object.

                              ### Sample Response:
                              {
                                  "Make":"Toyota",
                                  "Model":"highlux",
                                  "Odometer":"100000",
                                  "ManufacturerDate":"2000-05-29",
                                  "Price": extracted price as decimal converted to dollar,
                                  "Contact":"contact@example.com"
                              }
                              """),
                          new(AuthorRole.User, listingText)
                      };
        var settings = new OpenAIPromptExecutionSettings
                       {
                           ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                       };
        var result = await _completionService.GetChatMessageContentAsync(history, settings, _kernel);

        var response = result.Content;
        return JsonSerializer.Deserialize<Listing>(response);
    }

    public sealed class CurrencyPlugin
    {
        private static readonly Random _random = new();

        [KernelFunction,
         Description("Currency amount and returns the equivalent amount in USD")]
        public static decimal ConvertToDollar([Description("The ISO 4217 currency code")] string currencyCode,
                                              [Description("The amount of money to convert")] decimal amount)
            => amount * (decimal)(_random.NextDouble() * 2);
    }

    public record Listing(
        string Make,
        string Model,
        string Odometer,
        string ManufacturerDate,
        decimal Price,
        string Contact)
    {
        public override string ToString() => $"{Make} {Model} [{Odometer}/{ManufacturerDate}] - {Price} | {Contact}";
    }
}