using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Samples.Whisper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OVR.OpenVR;
using ReadyPlayerMe.Core;
using System.Threading.Tasks;
using Amazon.Polly.Model;
using Amazon.Polly;
using Amazon.Runtime;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using Amazon;


namespace OpenAI
{
    public class Chat : MonoBehaviour
    {
        [SerializeField] private Image progress;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject text;
        [SerializeField] private GameObject bar;

        public UnityEvent OnReplyReceived;

        private string response;
        private bool isDone = true;

        public List<ChatMessage> messages = new List<ChatMessage>();
        private readonly string fileName = "output.wav";
        private readonly int duration = 9;

        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi("");

        private void Start()
        {
            text.SetActive(false);
            bar.SetActive(false);
            var message = new ChatMessage
            {
                Role = "user",
                Content = "You are an astronaut and astronomer named Dr. Astro. " +
                "You will have an educational conversation where you will teach the user about exoplanets and the search for life on exoplanets. " +
                "Specifically, you must cover these 5 topics: " +
                "the transit method for detecting exoplanets, " +
                "the key criterion for a planet to be considered potentially habitable, " +
                "the radial velocity method for detecting exoplanets, " +
                "the significance of liquid water in the search for extraterrestrial life, " +
                "the space telescope most successful in discovering exoplanets using the transit method. " +
                "Please start the conversation with an adequate introduction of yourself and the topics that will be covered. " +
                "After each topic, you should allow for questions and respond to them but after the user has no more questions you must move on to the next topic." +
                "Please limit the contents of the conversational interaction to these topics or not further than astronomy in general. " +
                "If the user asks unrelated questions or makes off-topic comments, please steer the conversation back to the topics above."
            };

         //   Debug.Log($"Starting message: {message.Content}");

            //messages.Add(message);
    // var recording = await StartRecording();
         // message.Content = message.Content + " " + recording;
            messages.Add(message);
            Debug.Log($"Starting message: {message.Content}");
            SendReply(message.Content);
        }

        public async Task<string> StartRecording()
        {
            Debug.Log("Start recording...");
            bar.SetActive(true);
            text.SetActive(true);
           
            isRecording = true;
            clip = Microphone.Start(null, false, duration, 44100);
            await Task.Delay(duration * 1000);
            Debug.Log("Stop recording...");
            text.SetActive(false);
            bar.SetActive(false);
            Microphone.End(null);
            byte[] data = SaveWav.Save(fileName, clip);

            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                // File = Application.persistentDataPath + "/" + fileName,
                Model = "whisper-1",
                Language = "en"
            };
            var res = await openai.CreateAudioTranscription(req);

            return res.Text;
        }

        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progress.fillAmount = time / duration;
            }

            if (time >= duration)
            {
                time = 0;
                progress.fillAmount = 0;
                isRecording = false;
                Microphone.End(null);
            }
        }    

        public void SendReply(string input)
        {
            var message = new ChatMessage()
            {
                Role = "user",
                Content = input
            };

            Debug.Log($"Sending message: {message.Content}");
            messages.Add(message);

            openai.CreateChatCompletionAsync(new CreateChatCompletionRequest()
            {
                Model = "gpt-4o-mini",
                Messages = messages
            }, OnResponse, OnComplete, new CancellationTokenSource());
        }

        private void OnResponse(List<CreateChatCompletionResponse> responses)
        {
            var text = string.Join("", responses.Select(r => r.Choices[0].Delta.Content));

            if (text == "") return;

            if (text.Contains("END_CONVO"))
            {
                text = text.Replace("END_CONVO", "");

                Invoke(nameof(EndConvo), 5);
            }

            var message = new ChatMessage()
            {
                Role = "assistant",
                Content = text
            };

            if (isDone)
            {
                OnReplyReceived.Invoke();
                isDone = false;
            }

            response = text;
        }

        private async void OnComplete()
        {
            var message = new ChatMessage()
            {
                Role = "assistant",
                Content = response
            };

            Debug.Log($"Complete response: {response}");

            messages.Add(message);
            await MakeAudioRequest(response);

            isDone = true;
            response = "";
        }


        private void EndConvo()
        {
            Debug.Log("Conversation ended.");
            messages.Clear();
        }



        public async Task MakeAudioRequest(string message)
        {
            var credentials = new BasicAWSCredentials("", "");
            var client = new AmazonPollyClient(credentials, RegionEndpoint.USEast1);

            var request = new SynthesizeSpeechRequest()
            {
                Text = message,
                Engine = Engine.Neural,
                VoiceId = VoiceId.Ayanda,
                OutputFormat = OutputFormat.Mp3
            };

            var response = await client.SynthesizeSpeechAsync(request);

            WriteIntoFile(response.AudioStream);

            string audioPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        audioPath = $"jar:file://{Application.persistentDataPath}/audio.mp3";
#elif (UNITY_IOS || UNITY_OSX) && !UNITY_EDITOR
        audioPath = $"file://{Application.persistentDataPath}/audio.mp3";
#else
            audioPath = $"{Application.persistentDataPath}/audio.mp3";
#endif

            using (var www = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.MPEG))
            {
                var op = www.SendWebRequest();

                while (!op.isDone) await Task.Yield();

                var clip = DownloadHandlerAudioClip.GetContent(www);

                audioSource.clip = clip;
                audioSource.Play();              
            }
     
            while (audioSource.isPlaying)
                await Task.Yield(); 
            var humanChat = await StartRecording();
            SendReply(humanChat);
        }

        private IEnumerator WaitForAudioToFinish()
        {
           
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }
        private void WriteIntoFile(Stream stream)
        {
            using (var fileStream = new FileStream($"{Application.persistentDataPath}/audio.mp3", FileMode.Create))
            {
                byte[] buffer = new byte[8 * 1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }
        }
    }
}


