using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace IotMojo
{
    public class MicrosoftCognitiveSpeechService
    {

        public async Task<string> Transcribe(Stream audiostream)
        {
            var requestUri = @"http://bingaudioconverter.azurewebsites.net/api/Converter";

            using (var client = new HttpClient())
            {
                audiostream.Position = 0;
                var response = await client.PostAsync(requestUri, new StreamContent(audiostream));
                var responseString = response.Content.ReadAsStringAsync().Result;
                try
                {
                    dynamic data = JsonConvert.DeserializeObject(responseString);
                    return data;
                }
                catch (JsonReaderException ex)
                {
                    throw new Exception(responseString, ex);
                }
            }
        }
        /// <summary>
        /// Gets text from an audio stream.
        /// </summary>
        /// <param name="audiostream"></param>
        /// <returns>Transcribed text. </returns>
        public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            var requestUri = @"https://speech.platform.bing.com/recognize?scenarios=ulm&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5&locale=en-US&device.os=bot&form=BCSSTT&version=3.0&format=json&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3&requestid=" + Guid.NewGuid();

            using (var client = new HttpClient())
            {
                var token = new Authentication("4a47c14e828f40b8801c67212766e0f5").GetAccessToken();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                using (var binaryContent = new ByteArrayContent(StreamToBytes(audiostream)))
                {
                    binaryContent.Headers.TryAddWithoutValidation("content-type", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");

                    var response = client.PostAsync(requestUri, binaryContent).Result;
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(responseString);
                        return data.header.name;
                    }
                    catch (JsonReaderException ex)
                    {
                        throw new Exception(responseString, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Converts Stream into byte[].
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Output byte[]</returns>
        private static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
