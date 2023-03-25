using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using OpenAI.GPT3.ObjectModels.SharedModels;
using Microsoft.CognitiveServices.Speech.Translation;

namespace OpenAI_Conversation_Demo
{
    internal class Program
    {
        static SpeechConfig config = CreateConfig();
        static SpeechTranslationConfig translationConfig = CreateTranslationConfig();

        public static async Task<string> RecognizeSpeechAsync()
        {
            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something...");

                // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                var result = await recognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"We recognized: {result.Text}");
                    return await Task.FromResult(result.Text);
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }

                return string.Empty;
            }
        }

        private static SpeechConfig CreateConfig()
        {
            var config = SpeechConfig.FromSubscription("7288c10f030b4911a55596df1c605599", "eastus");
            config.SpeechRecognitionLanguage = "ro-RO";
            config.SpeechSynthesisLanguage = "ro";
            config.SpeechSynthesisVoiceName = "ro-RO-EmilNeural";
            return config;
        }

        private static SpeechTranslationConfig CreateTranslationConfig()
        {
            var speechTranslationConfig = SpeechTranslationConfig.FromSubscription("7288c10f030b4911a55596df1c605599", "eastus");
            speechTranslationConfig.SpeechRecognitionLanguage = "ro-RO";
            speechTranslationConfig.AddTargetLanguage("en");

            return speechTranslationConfig;
        }


        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            var apiKey = "85e3ba53e7a94f999e0d602b6a1775da";

            var gpt3 = new OpenAIService(new OpenAiOptions()
            {
                ProviderType = ProviderType.Azure,
                ApiKey = apiKey,
                DeploymentId = "azure-openai-davinci",
                ResourceName = "azure-openai-hackathon-accessability",
            });

            while (true)
            {
                var prompt =  await RecognizeSpeechAsync(); //await GetTranslatedSpeechAsync();

                if (prompt == string.Empty)
                {
                    continue;
                }

                var completionResult = await gpt3.Completions.CreateCompletion(new CompletionCreateRequest()
                {
                    Prompt = $"{prompt}. Răspunde pentru un copil de 10 ani care vrea să învețe despre subiect. Nu repeta același cuvânt de prea multe ori.",
                    Model = Models.ChatGpt3_5Turbo,
                    Temperature = 0.5F,
                    MaxTokens = 1000,
                    N = 1
                });

                if (completionResult.Successful)
                {
                    foreach (var choice in completionResult.Choices)
                    {
                        Console.WriteLine(choice.Text);
                        await SpeakText(choice.Text);
                    }
                }
                else
                {
                    Console.WriteLine($"{completionResult.Error?.Code}: {completionResult.Error?.Message}");
                }
            }

            Console.ReadKey(true);
        }

        private static async Task<string> GetTranslatedSpeechAsync()
        {
            Console.WriteLine("Speak into your microphone.");
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var translationRecognizer = new TranslationRecognizer(translationConfig, audioConfig);

            var translationRecognitionResult = await translationRecognizer.RecognizeOnceAsync();
            switch (translationRecognitionResult.Reason)
            {
                case ResultReason.TranslatedSpeech:
                    Console.WriteLine($"RECOGNIZED: Text={translationRecognitionResult.Text}");
                    foreach (var element in translationRecognitionResult.Translations)
                    {
                        Console.WriteLine($"TRANSLATED into '{element.Key}': {element.Value}");
                        return element.Value;
                    }
                    break;
                case ResultReason.NoMatch:
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(translationRecognitionResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
            }

            return string.Empty;
        }

        private static async Task SpeakText(string text)
        {
            using (var synthesizer = new SpeechSynthesizer(config))
            {
                using (var result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                }
            }
        }
    }
}