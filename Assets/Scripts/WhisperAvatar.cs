using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Samples.Whisper;
using Oculus.Platform;

namespace OpenAI
{
    public class WhisperAvatar : MonoBehaviour
    {
        [SerializeField] private ChatTest chatTest;
        [SerializeField] private Image progress;

        private readonly string fileName = "output.wav";
        private readonly int duration = 9;

        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi("sk-proj-pjkgZjpAYVAtiofl2HVGT3BlbkFJThMCNqZYHeJnAJOmSlUu");

        private void Start()
        {
            StartRecording();
        }

        public async void StartRecording()
        {
            if (isRecording)
            {
                isRecording = false;
                Debug.Log("Stop recording...");

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

                chatTest.SendReply(res.Text);
            }
            else
            {
                Debug.Log("Start recording...");
                isRecording = true;
                clip = Microphone.Start(null, false, duration, 44100);
            }
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
    }
}