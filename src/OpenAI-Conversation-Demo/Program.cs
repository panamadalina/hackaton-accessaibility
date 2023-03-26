using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Diagnostics;

namespace OpenAI_Conversation_Demo
{
    internal class Program
    {
        /// <summary>
        /// Codes representing keyboard keys.
        /// </summary>
        /// <remarks>
        /// Key code documentation:
        /// http://msdn.microsoft.com/en-us/library/dd375731%28v=VS.85%29.aspx
        /// </remarks>
        internal enum KeyCode
        {
            LControlKey = 162,
        }

        /// <summary>
        /// Provides keyboard access.
        /// </summary>
        internal static class NativeKeyboard
        {
            /// <summary>
            /// A positional bit flag indicating the part of a key state denoting
            /// key pressed.
            /// </summary>
            private const int KeyPressed = 0x8000;

            /// <summary>
            /// Returns a value indicating if a given key is pressed.
            /// </summary>
            /// <param name="key">The key to check.</param>
            /// <returns>
            /// <c>true</c> if the key is pressed, otherwise <c>false</c>.
            /// </returns>
            public static bool IsKeyDown(KeyCode key)
            {
                return (GetKeyState((int)key) & KeyPressed) != 0;
            }

            /// <summary>
            /// Gets the key state of a key.
            /// </summary>
            /// <param name="key">Virtuak-key code for key.</param>
            /// <returns>The state of the key.</returns>
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern short GetKeyState(int key);
        }

        static SpeechConfig config = CreateConfig();
        static SpeechTranslationConfig translationConfig = CreateTranslationConfig();

        public static async Task<string> RecognizeSpeechAsync()
        {
            Console.Beep();

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

        static SpeechSynthesizer synthesizer = new SpeechSynthesizer(config);
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

            await SpeakText(synthesizer, "Bună! Poți să îmi pui întrebări după ce auzi un bip.");

            while (true)
            {
                var prompt = await RecognizeSpeechAsync(); //await GetTranslatedSpeechAsync();

                var stopCommands = new[] { "Stop", "Mersi", "Mulțumesc", "Suficient", "De ajuns", "Op." }.Select(s => s.ToUpperInvariant());

                if (stopCommands.Any(stop => prompt.ToUpperInvariant().StartsWith(stop)))
                {
                    SpeakText(synthesizer, "Ne mai auzim!");
                    break;
                }

                if (prompt == string.Empty)
                {
                    continue;
                }

                var completionResult = await gpt3.Completions.CreateCompletion(new CompletionCreateRequest()
                {
                    Prompt = $"{prompt}. Răspunde pentru un copil de 10 ani care vrea să învețe despre subiect. Nu repeta același cuvânt de prea multe ori.",
                    Model = Models.Gpt4,
                    Temperature = 0.3F,
                    MaxTokens = 1000,
                    N = 1
                });

                if (completionResult.Successful)
                {
                    foreach (var choice in completionResult.Choices)
                    {
                        Console.WriteLine(choice.Text);

                        var speaking = SpeakText(synthesizer, choice.Text);
                        var mre = new ManualResetEventSlim(false);
                        var cancelling = Task.Run(() =>
                        {
                            while (true)
                            {
                                if (NativeKeyboard.IsKeyDown(KeyCode.LControlKey))
                                {
                                    Debug.WriteLine("control pressed");
                                    break;
                                }
                                
                                if (mre.Wait(15))
                                {
                                    break;
                                }
                            }
                        });

                        Task.WaitAny(speaking, cancelling);
                        if (cancelling.IsCompleted)
                        {
                            synthesizer.StopSpeakingAsync();
                        }
                        else
                        {
                            mre.Wait(15);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{completionResult.Error?.Code}: {completionResult.Error?.Message}");
                }
            }
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

        private static async Task SpeakText(SpeechSynthesizer synthesizer, string text)
        {
            using var result = await synthesizer.SpeakTextAsync(text);
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