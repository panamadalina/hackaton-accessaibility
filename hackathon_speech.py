import os
import azure.cognitiveservices.speech as speechsdk
import openai

import winsound
import keyboard

# while True:
#     # do something
#     if keyboard.is_pressed("q"):
#         print("q pressed, ending loop")
#         break
# from ro_diacritics import restore_diacritics

# os.environ["SPEECH_KEY"] = "7288c10f030b4911a55596df1c605599"
# os.environ["SPEECH_REGION"] = "eastus"
# os.environ["OPEN_AI_KEY"] = "965ec6b0fc474282a0ea09a54d8932d9"
# os.environ["OPEN_AI_ENDPOINT"] = "https://azure-openai-hackathon-accessability.openai.azure.com/"

# This example requires environment variables named "OPEN_AI_KEY" and "OPEN_AI_ENDPOINT"
# Your endpoint should look like the following https://YOUR_OPEN_AI_RESOURCE_NAME.openai.azure.com/
openai.api_key = "965ec6b0fc474282a0ea09a54d8932d9"
openai.api_base =  "https://azure-openai-hackathon-accessability.openai.azure.com/"
openai.api_type = 'azure'
openai.api_version = '2022-12-01'

# This will correspond to the custom name you chose for your deployment when you deployed a model.
deployment_id='azure-openai-davinci'

# This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
speech_config = speechsdk.SpeechConfig(subscription="7288c10f030b4911a55596df1c605599", region="eastus")
audio_output_config = speechsdk.audio.AudioOutputConfig(use_default_speaker=True)
audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)

# Should be the locale for the speaker's language.
speech_config.speech_recognition_language="ro-RO"
speech_recognizer = speechsdk.SpeechRecognizer(speech_config=speech_config, audio_config=audio_output_config)

# The language of the voice that responds on behalf of Azure OpenAI.
speech_config.speech_synthesis_voice_name='ro-RO-EmilNeural'
speech_synthesizer = speechsdk.SpeechSynthesizer(speech_config=speech_config, audio_config=audio_config)

use_case = 1
user_name = "input user name"

# Prompts Azure OpenAI with a request and synthesizes the response.
def ask_openai(prompt):
    # Adding custome profile context for prompt
    # age
    # interaction_type
    # profile = "copil"
    # age = 10
    # interaction_type = "poveste scurta"
    # prompt_format = "Adauga o lista numerotata de posibile intrebari pentru utilizator:"
    
    input_prompt = prompt + ". Adaptează răspunsul pentru limbajul unui copil de 10 ani. Nu repeta același cuvânt de prea multe ori."
    # Ask Azure OpenAI
    response = openai.Completion.create(engine=deployment_id, prompt=input_prompt, max_tokens=500)
    text = response['choices'][0]['text'].replace('\n', '').replace(' .', '.').strip()
    print('Azure OpenAI response:' + text)

    # Azure text-to-speech output
    speech_synthesis_result = speech_synthesizer.speak_text_async(text).get()

    # Check result
    if speech_synthesis_result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
        print("Speech synthesized to speaker for text [{}]".format(text))

    elif speech_synthesis_result.reason == speechsdk.ResultReason.Canceled:
        cancellation_details = speech_synthesis_result.cancellation_details
        print("Speech synthesis canceled: {}".format(cancellation_details.reason))
        if cancellation_details.reason == speechsdk.CancellationReason.Error:
            print("Error details: {}".format(cancellation_details.error_details))

# Continuously listens for speech input to recognize and send as text to Azure OpenAI
def chat_with_open_ai():
    while True:
        print("Azure OpenAI is listening. Say 'Stop' or press Ctrl-Z to end the conversation.")
        # Play a Beep sound to indicate that Azure OpenAI is listening.
        winsound.PlaySound("SystemAsterisk", winsound.SND_ALIAS)
        # speech_synthesizer.speak_text_async("Spune-mi cu ce te pot ajuta, " + user_name).get()
        try:
            # Get audio from the microphone and then send it to the TTS service.
            speech_recognition_result = speech_recognizer.recognize_once_async().get()
            
            # If speech is recognized, send it to Azure OpenAI and listen for the response.
            if speech_recognition_result.reason == speechsdk.ResultReason.RecognizedSpeech:
                if speech_recognition_result.text == "Stop.": 
                    print("Conversation ended.")
                    break
                print("Recognized speech: {}".format(speech_recognition_result.text))
                ask_openai(speech_recognition_result.text)
                # speech_synthesizer.speak_text_async("Te rog să aștepți în timp ce caut un raspuns...").get()
            elif speech_recognition_result.reason == speechsdk.ResultReason.NoMatch:
                print("No speech could be recognized: {}".format(speech_recognition_result.no_match_details))
                break
            elif speech_recognition_result.reason == speechsdk.ResultReason.Canceled:
                cancellation_details = speech_recognition_result.cancellation_details
                print("Speech Recognition canceled: {}".format(cancellation_details.reason))
                if cancellation_details.reason == speechsdk.CancellationReason.Error:
                    print("Error details: {}".format(cancellation_details.error_details))
                    print("Did you set the speech resource key and region values?")
        except EOFError:
            break

# Main

try:
    chat_with_open_ai()
except Exception as err:
    print("Encountered exception. {}".format(err))