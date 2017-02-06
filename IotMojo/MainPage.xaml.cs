using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;




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
        SpeechRecognizer recognizer;
        MediaCapture _mediaCapture;
        IRandomAccessStream _audioStream;
        Timer timer;
        DispatcherTimer uiUpdate;
        bool sttReceived = false;
        string receivedText = string.Empty;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += OnLoaded;
            InitMediaCapture();
            

            uiUpdate = new DispatcherTimer();
            uiUpdate.Interval = new TimeSpan(0, 0, 1);
            uiUpdate.Tick += UiUpdate_Tick;
        }

        private void UiUpdate_Tick(object sender, object e)
        {
            if(sttReceived)
            {
                txtQuery.Text = receivedText;
                uiUpdate.Stop();
                GetAnswer();
                //if (recognizer.State == SpeechRecognizerState.Idle || recognizer.State == SpeechRecognizerState.Paused)
                //{
                //    recognizer.ContinuousRecognitionSession.Resume();
                //}
            }
        }

        private void InitTimer()
        {
            timer = new Timer(new TimerCallback(OnStopListening),
                                           this,
                                           TimeSpan.FromSeconds(5),
                                           TimeSpan.FromMilliseconds(-1));

            uiUpdate.Start();
        }

        private void OnStopListening(object state)
        {
            StopListening();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            recognizer = new SpeechRecognizer();

            var commands = new Dictionary<string, int>()
            {
                ["bablu"] = 1
            };
            recognizer.Constraints.Add(new SpeechRecognitionListConstraint(commands.Keys));

            await recognizer.CompileConstraintsAsync();

            recognizer.ContinuousRecognitionSession.ResultGenerated +=
              async (s, e) =>
              {
                  if ((e.Result != null) && (commands.ContainsKey(e.Result.Text)))
                  {
                      await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtData.Text = "Listening";
                            ReadOutText();
                            //recognizer.ContinuousRecognitionSession.PauseAsync();
                            Listen();
                        }
                     );
                     recognizer.ContinuousRecognitionSession.Resume();
                  }
              };

            await recognizer.ContinuousRecognitionSession.StartAsync(SpeechContinuousRecognitionMode.PauseOnRecognition);
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            GetAnswer();
        }

        private async void GetAnswer()
        {
            if (!string.IsNullOrEmpty(txtQuery.Text))
            {
                var answered = false;
                var wikiQuery = QueryLuis(txtQuery.Text);
                if (!string.IsNullOrEmpty(wikiQuery))
                {
                    var responseData = QueryWiki(wikiQuery);
                    if (!string.IsNullOrEmpty(responseData))
                    {
                        txtData.Text = responseData;
                        //CortanaAudio(txtData.Text);
                    }
                    else
                    {
                        txtData.Text = "No information found";
                    }
                    answered = true;
                }
                else
                {
                    txtData.Text = QueryApiAi(txtQuery.Text);
                    answered = !string.IsNullOrEmpty(txtData.Text);
                }

                if(!answered)
                {
                    txtData.Text = "No information found";
                }
            }
            else
            {
                txtData.Text = "Unable to get you";
            }
            await ReadOutText();
        }

        private async System.Threading.Tasks.Task ReadOutText()
        {
            
            recognizer.ContinuousRecognitionSession.PauseAsync();
            
            SpeechSynthesizer synt = new SpeechSynthesizer();
            SpeechSynthesisStream syntStream = await synt.SynthesizeTextToStreamAsync(txtData.Text);
            mediaElement.SetSource(syntStream, syntStream.ContentType);
            if (recognizer.State == SpeechRecognizerState.Idle || recognizer.State == SpeechRecognizerState.Paused)
            {
                recognizer.ContinuousRecognitionSession.Resume();
            }
            
        }

        private string QueryLuis(string query)
        {
            if(query[query.Length-1] == '.')
            {
                query = query.Remove(query.Length - 1);
            }
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
                            if (entityType.Contains("builtin.encyclopedia") || entityType.Contains("builtin.geography"))
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

        private string QueryApiAi(string query)
        {
            try
            {
                string uri = $"https://api.api.ai/v1/query?v=20150910&query={query}&lang=en&sessionId=1234567890";
                using (var aiCLient = new HttpClient())
                {
                    aiCLient.DefaultRequestHeaders.Add("Authorization", "Bearer d6fcde5fcad846b3a26f25827bbd06a3");
                    var response = aiCLient.GetAsync(new Uri(uri));
                    var dataString = response.Result.Content.ReadAsStringAsync();

                    dynamic data = JsonConvert.DeserializeObject(dataString.Result);
                    var intentName = data.result.metadata.intentName.ToString();
                    if (intentName == "wordmeaning" || intentName == "spell_intent")
                    {
                        if (data.result.parameters != null && data.result.parameters["dictionary"] != null && !string.IsNullOrEmpty(data.result.parameters["dictionary"].ToString()))
                        {
                            string result = data.result.parameters["dictionary"].ToString();

                            if (intentName == "spell_intent")
                            {
                                return string.Join(" ", result.ToCharArray()).ToUpper();
                            }
                            else
                            {
                                return QueryDictionary(result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
            return string.Empty;
        }

        private string QueryDictionary(string query)
        {
            try
            {
                string uri = $"https://od-api.oxforddictionaries.com:443/api/v1/entries/en/{query}/definitions";
                using (var aiCLient = new HttpClient())
                {
                    aiCLient.DefaultRequestHeaders.Add("app_id", "e664b07c");
                    aiCLient.DefaultRequestHeaders.Add("app_key", "3b355ce77502ab9fb7bdd67f8a60f11c");

                    var response = aiCLient.GetAsync(new Uri(uri));
                    var dataString = response.Result.Content.ReadAsStringAsync();

                    dynamic data = JsonConvert.DeserializeObject(dataString.Result);

                    if (data.results != null
                        && data.results[0] != null
                        && data.results[0].lexicalEntries!=null
                        && data.results[0].lexicalEntries[0] != null
                        && data.results[0].lexicalEntries[0].entries != null
                        && data.results[0].lexicalEntries[0].entries[0] != null
                        && data.results[0].lexicalEntries[0].entries[0].senses != null
                        && data.results[0].lexicalEntries[0].entries[0].senses[0] != null
                        && data.results[0].lexicalEntries[0].entries[0].senses[0].definitions!=null
                        && data.results[0].lexicalEntries[0].entries[0].senses[0].definitions[0] != null)
                    {
                        return data.results[0].lexicalEntries[0].entries[0].senses[0].definitions[0].ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                return string.Empty;
            }
            return string.Empty;
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

        #region Google STT

        private async void InitMediaCapture()
        {
            _mediaCapture = new MediaCapture();
            var captureInitSettings = new MediaCaptureInitializationSettings();
            captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            await _mediaCapture.InitializeAsync(captureInitSettings);
            _mediaCapture.Failed += MediaCaptureOnFailed;
            
            //_mediaCapture.RecordLimitationExceeded += MediaCaptureOnRecordLimitationExceeded;
        }

        private async void Listen()
        {
            try
            {
                recognizer.ContinuousRecognitionSession.PauseAsync();
                _audioStream = new InMemoryRandomAccessStream();
                InitTimer();
                sttReceived = false;

                var localizationDirectory = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(@"Assets");
                StorageFile sampleFile = await localizationDirectory.GetFileAsync("audio.flac");
                //var stream = await sampleFile.OpenReadAsync();

                var profile = await MediaEncodingProfile.CreateFromFileAsync(sampleFile);

                await _mediaCapture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.High), _audioStream);
                //await _mediaCapture.StartRecordToStreamAsync(profile, _audioStream);
            }
            catch (Exception ex)
            {
                //txtData.Text = ex.ToString();
                txtData.Text = "Something went wrong. Please try again";
                ReadOutText();
            }
        }

        #endregion

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            Listen();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopListening();
        }

        private async void StopListening()
        {
            try
            {
                
                await _mediaCapture.StopRecordAsync();
                //txtData.Text = "Thinking about the answer";
                //ReadOutText();
                var result = await new MicrosoftCognitiveSpeechService().Transcribe(_audioStream.AsStream());
                //var result = await new GoogleCognitiveSpeechService().GetTextFromAudioAsync(_audioStream.AsStream());
                //var result = await new WatsonSpeechToText().GetTextFromAudioAsync(_audioStream.AsStream());

                receivedText = result;
                //_mediaCapture.Dispose();
                sttReceived = true;
            }
            catch (Exception ex)
            {
                receivedText = "Something went wrong. Please try again";
                sttReceived = true;
                //txtData.Text = "Something went wrong. Please try again";
                //ReadOutText();
            }
        }

        private async void MediaCaptureOnFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var warningMessage = new MessageDialog(String.Format("The audio capture failed: {0}", errorEventArgs.Message), "Capture Failed");
                await warningMessage.ShowAsync();
            });
        }
    }
}
