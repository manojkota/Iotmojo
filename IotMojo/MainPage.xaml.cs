using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotMojo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        HttpClient client = new HttpClient();
        string accessToken;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtQuery.Text))
            {
                var wikiQuery = QueryLuis(txtQuery.Text);
                if(!string.IsNullOrEmpty(wikiQuery))
                {
                    var responseData = QueryWiki(wikiQuery);
                    if(!string.IsNullOrEmpty(responseData))
                    {
                        txtData.Text = responseData;
                        //CortanaAudio(txtData.Text);

                        string Ssml =
        @"<speak version='1.0' " +
        "xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
        "Hello <prosody contour='(0%,+80Hz) (10%,+80%) (40%,+80Hz)'>World</prosody> " +
        "<break time='500ms' />" +
        "Goodbye <prosody rate='slow' contour='(0%,+20Hz) (10%,+30%) (40%,+10Hz)'>World</prosody>" +
        "</speak>";

                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream = await synt.SynthesizeTextToStreamAsync(txtData.Text);
                        mediaElement.SetSource(syntStream, syntStream.ContentType);

                    }
                    else
                    {
                        txtData.Text = "No information found";
                    }
                }
                else
                {
                    txtData.Text = "No information found";
                }
            }
        }

        private string QueryLuis(string query)
        {
            var entity = string.Empty;
            var luisUrl = new Uri($"https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/49793a7f-9da1-444a-8d65-ee5008378030?subscription-key=bcc34292c7e54855a02835a097fbc1e4&q={query}&verbose=true");
            var response = client.GetAsync(luisUrl);
            var dataString = response.Result.Content.ReadAsStringAsync();
            dynamic luisResponse = JsonConvert.DeserializeObject(dataString.Result);
            if (luisResponse != null)
            {
                if (luisResponse.topScoringIntent != null && luisResponse.topScoringIntent.intent !=null && luisResponse.topScoringIntent.intent.ToString() == "edu_intent")
                {
                    if (luisResponse.entities != null)
                    {
                        for (int i = 0; i < luisResponse.entities.Count; i++)
                        {
                            var entityType = luisResponse.entities[i].type.ToString();
                            if (entityType.Contains("builtin.encyclopedia"))
                            {
                                entity = luisResponse.entities[i].entity.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            return entity;
        }

        private string QueryWiki(string query)
        {
            var data = string.Empty;
            var wikiUrl = new Uri($"https://en.wikipedia.org/api/rest_v1/page/summary/{query}");
            var response = client.GetAsync(wikiUrl);
            var dataString = response.Result.Content.ReadAsStringAsync();
            dynamic wikiResponse = JsonConvert.DeserializeObject(dataString.Result);
            if (wikiResponse != null)
            {
                if (wikiResponse.extract != null && !string.IsNullOrEmpty(wikiResponse.extract.ToString()))
                {
                    string wikiData = wikiResponse.extract.ToString();
                    wikiData = wikiData.Replace("\n", " ");
                    data = wikiData;
                }
            }
            return data;
        }

        private void CortanaAudio(string inputText)
        {
            Authentication auth = new Authentication("4a47c14e828f40b8801c67212766e0f5");
            try
            {
                accessToken = auth.GetAccessToken();
            }
            catch (Exception ex)
            {
                return;
            }

            string requestUri = "https://speech.platform.bing.com/synthesize";

            var cortana = new Synthesize(new Synthesize.InputOptions()
            {
                RequestUri = new Uri(requestUri),
                // Text to be spoken.
                Text = inputText,
                VoiceType = Gender.Female,
                // Refer to the documentation for complete list of supported locales.
                Locale = "en-US",
                // You can also customize the output voice. Refer to the documentation to view the different
                // voices that the TTS service can output.
                VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
                // Service can return audio in different output format. 
                OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                AuthorizationToken = "Bearer " + accessToken,
            });

            cortana.OnAudioAvailable += PlayAudio;
            cortana.OnError += ErrorHandler;
            cortana.Speak(CancellationToken.None).Wait();
        }

        /// <summary>
        /// This method is called once the audio returned from the service.
        /// It will then attempt to play that audio file.
        /// Note that the playback will fail if the output audio format is not pcm encoded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="GenericEventArgs{Stream}"/> instance containing the event data.</param>
        private void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            mediaElement.SetSource(args.EventData.AsRandomAccessStream(),"audio/wav");
            mediaElement.Play();
            //SoundPlayer player = new SoundPlayer(args.EventData);
            //player.PlaySync();
            args.EventData.Dispose();
        }

        
        private void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            txtData.Text = string.Format("Unable to complete the TTS request: [{ 0}]", e.ToString());
            //Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }
    }
}
