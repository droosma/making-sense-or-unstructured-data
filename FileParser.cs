using System.Text.Json;

using Microsoft.SemanticKernel;

public class FileParser(IKernelBuilder kernelBuilder)
{
    private readonly Kernel _kernel = kernelBuilder.Build();

    public async Task Execute(string contents)
    {
        var chunks = GetChunks(contents);

        var listingTextTasks = chunks.AsParallel()
                                     .Select(ExtractListings)
                                     .ToList();
        var listingTexts = await Task.WhenAll(listingTextTasks);

        var listingTasks = listingTexts.SelectMany(listingText => listingText)
                                       .AsParallel()
                                       .Select(ExtractListing)
                                       .ToList();
        var listings = await Task.WhenAll(listingTasks);

        Console.WriteLine(string.Join<Listing>(Environment.NewLine, listings));
    }

    private async Task<IEnumerable<string>> ExtractListings(string contents)
    {
        var arguments = new KernelArguments(new PromptExecutionSettings {ModelId = "gpt-35"}) {{"content", contents}};
        var result = await _kernel.InvokePromptAsync("""
                                                     You are tasked with splitting a large text into individual blocks, each describing a single car listings. Below is the text content from a file:
                                                     ```
                                                     {{$content}}
                                                     ```

                                                     ### Tasks:

                                                     1. Ensure no information is omitted. Include all text as it appears in the file.
                                                     2. Produce the output in valid JSON format. The output must be directly parsable into an Array of Strings, each string representing a single listings description.

                                                     ### Sample Output:
                                                     [
                                                         "Description text for the first listing",
                                                         "Description text for the second listing"
                                                     ]
                                                     """,
                                                     arguments);
        return JsonSerializer.Deserialize<IEnumerable<string>>(result.ToString());
    }

    private async Task<Listing> ExtractListing(string contents)
    {
        var arguments = new KernelArguments(new PromptExecutionSettings { ModelId = "gpt-35" }) { { "content", contents } };
        var result = await _kernel.InvokePromptAsync("""
                                                     You are tasked with converting a car listing into a structured JSON format below is the text content from a file:
                                                     ```
                                                     {{$content}}
                                                     ```

                                                     ### Tasks:

                                                     1. Ensure no information is omitted. Include all text as it appears in the file.
                                                     2. Produce the output in valid JSON format. The output must be directly parsable into an Listing object.

                                                     ### Sample Output:
                                                     {
                                                         "Make":"Toyota",
                                                         "Model":"highlux",
                                                         "Odometer":"100000",
                                                         "ManufacturerDate":"2000-05-29",
                                                         "Price":"16900",
                                                         "Contact":"contact@example.com"
                                                     }
                                                     """,
                                                     arguments);
        var response = result.ToString();
        return JsonSerializer.Deserialize<Listing>(response);
    }

    private static IEnumerable<string> GetChunks(string content,
                                                 int chunkSize = 100,
                                                 int overlapLines = 10)
    {
        var lines = content.Split(Environment.NewLine);
        return Enumerable.Range(0, (lines.Length + chunkSize - 1) / chunkSize)
                         .Select(i => string.Join(Environment.NewLine,
                                                  lines.Skip(i * chunkSize)
                                                       .Take(chunkSize + overlapLines)));
    }

    public record Listing(
        string Make,
        string Model,
        string Odometer,
        string ManufacturerDate,
        string Price,
        string Contact)
    {
        public override string ToString() => $"{Make} {Model} [{Odometer}/{ManufacturerDate}] - {Price} | {Contact}";
    }
}