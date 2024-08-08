using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace OpenAI
{
    public class ChatTest : MonoBehaviour
    {
        [SerializeField] private TextToSpeech textToSpeech;
        [SerializeField] private WhisperAvatar whisperAvatar;

        public UnityEvent OnReplyReceived;

        private string response;
        private bool isDone = true;

        private OpenAIApi openai = new OpenAIApi("sk-proj-pjkgZjpAYVAtiofl2HVGT3BlbkFJThMCNqZYHeJnAJOmSlUu");

        public List<ChatMessage> messages = new List<ChatMessage>();

        private void Start()
        {
            var message = new ChatMessage
            {
                Role = "user",
                Content = "You are an astronaut who is an expert on astronomy as well. Please engage me with some information about space and astronomy. Please do not respond to this first message."
            };

            Debug.Log($"Starting message: {message.Content}");

            messages.Add(message);

            SendReply(message.Content);
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
                Model = "gpt-3.5-turbo",
                Messages = messages
            }, OnResponse, OnComplete, new CancellationTokenSource());
        }

        private void OnResponse(List<CreateChatCompletionResponse> responses)
        {
            var text = string.Join("", responses.Select(r => r.Choices[0].Delta.Content));

           // Debug.Log($"Received response: {text}");

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

        private void OnComplete()
        {
            var message = new ChatMessage()
            {
                Role = "assistant",
                Content = response
            };

            Debug.Log($"Complete response: {response}");

            messages.Add(message);

          //  textToSpeech.MakeAudioRequest(response, OnAudioPlaybackComplete);
            textToSpeech.MakeAudioRequest(response, whisperAvatar.StartRecording);


            isDone = true;
            response = "";
        }

        //private void OnAudioPlaybackComplete()
        //{
        //    whisperAvatar.StartRecording();
        //}

        private void EndConvo()
        {
            Debug.Log("Conversation ended.");
            messages.Clear();
        }
    }
}
