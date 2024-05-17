using Microsoft.SemanticKernel;

var kernelBuilder = Kernel.CreateBuilder()
                          .AddAzureOpenAIChatCompletion(deploymentName:"gpt-35-turbo",
                                                        endpoint:"",
                                                        apiKey:"",
                                                        serviceId:"gpt-35",
                                                        modelId:"gpt-35")
                          .AddAzureOpenAIChatCompletion(deploymentName:"gpt-4",
                                                        endpoint:"",
                                                        apiKey:"",
                                                        serviceId:"gpt-4",
                                                        modelId:"gpt-4");

#region FileParser

var fileParser = new FileParser(kernelBuilder);
var fileContents = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "importFile.txt"));

await fileParser.Execute(fileContents);

#endregion

#region ParseWithFunctions

// Uncomment to try out Functions

//var chatFileParser = new ChatFileParser(kernelBuilder);
//var chatFileParserResult = await chatFileParser.ExtractListing("""
//                                                               I'm selling my beloved Toyota Camry. It's a fantastic car with only 100,000 miles on it. Manufactured back in October 2015. I'm looking to get € 12,500 for it. Let me know if you're interested!

//                                                               Contact me at: example123@email.com
//                                                               """);

#endregion


Console.ReadLine();