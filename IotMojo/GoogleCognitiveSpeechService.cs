using Google.Apis.CloudSpeechAPI.v1beta1.Data;
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
    public class GoogleCognitiveSpeechService
    {
        /// <summary>
        /// Gets text from an audio stream.
        /// </summary>
        /// <param name="audiostream"></param>
        /// <returns>Transcribed text. </returns>
        public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            var requestUri = @"https://speech.googleapis.com/v1beta1/speech:syncrecognize?key=AIzaSyBmkQCRCoqCxhkiioxspVc6iWXX0KC569w";

            using (var client = new HttpClient())
            {

                //var localizationDirectory = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(@"Assets");
                //Windows.Storage.StorageFile sampleFile = await localizationDirectory.GetFileAsync("audio.raw");
                //var stream = await sampleFile.OpenReadAsync();

                var request = new SyncRecognizeRequest()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = "LINEAR16",
                        SampleRate = 16000,
                        LanguageCode = "en-US"
                    },
                    Audio = new RecognitionAudio()
                    {
                        Content = Convert.ToBase64String(ReadFully(audiostream))
                        //Content = Convert.ToBase64String(ReadFully(stream.AsStream()))
                    },
                    ETag = null
                };
                JsonSerializerSettings sett = new JsonSerializerSettings();
                sett.NullValueHandling = NullValueHandling.Ignore;
                var myContent = JsonConvert.SerializeObject(request, sett);
                var buffer = Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                var response = await client.PostAsync(requestUri, byteContent);
                var responseString = await response.Content.ReadAsStringAsync();
                try
                {
                    var data = JsonConvert.DeserializeObject<SyncRecognizeResponse>(responseString);
                    if (data != null && data.Results != null && data.Results.Any() && data.Results[0].Alternatives.Any())
                    {
                        return data.Results[0].Alternatives?.FirstOrDefault()?.Transcript;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                catch (JsonReaderException ex)
                {
                    throw new Exception(responseString, ex);
                }

            }
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
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
