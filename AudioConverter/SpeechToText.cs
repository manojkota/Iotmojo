using Microsoft.Bing.Speech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace AudioConverter
{
    public class SpeechToText
    {
        private string convertedText = string.Empty;

        /// <summary>
        /// Short phrase mode URL
        /// </summary>
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");

        /// <summary>
        /// The long dictation URL
        /// </summary>
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");

        /// <summary>
        /// A completed task
        /// </summary>
        private static readonly Task CompletedTask = Task.FromResult(true);

        /// <summary>
        /// Cancellation token used to stop sending the audio.
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Invoked when the speech client receives a partial recognition hypothesis from the server.
        /// </summary>
        /// <param name="args">The partial response recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        //public Task OnPartialResult(RecognitionPartialResult args)
        //{
        //    Console.WriteLine("--- Partial result received by OnPartialResult ---");

        //    // Print the partial response recognition hypothesis.
        //    Console.WriteLine(args.DisplayText);

        //    Console.WriteLine();

        //    return CompletedTask;
        //}

        /// <summary>
        /// Invoked when the speech client receives a phrase recognition result(s) from the server.
        /// </summary>
        /// <param name="args">The recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        public Task OnRecognitionResult(RecognitionResult args)
        {
            var response = args;
            //Console.WriteLine();

            //Console.WriteLine("--- Phrase result received by OnRecognitionResult ---");

            // Print the recognition status.
            //Console.WriteLine("***** Phrase Recognition Status = [{0}] ***", response.RecognitionStatus);
            if (response.Phrases != null)
            {
                foreach (var result in response.Phrases)
                {
                    // Print the recognition phrase display text.
                    //Console.WriteLine("{0} (Confidence:{1})", result.DisplayText, result.Confidence);
                    convertedText = result.DisplayText;
                }
            }

            //Console.WriteLine();
            return CompletedTask;
        }

        /// <summary>
        /// Sends a speech recognition request to the speech service
        /// </summary>
        /// <param name="audioFile">The audio file.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <returns>
        /// A task
        /// </returns>
        public async Task<string> Run(Stream audioFile, string locale, string subscriptionKey)
        {
            // create the preferences object
            var preferences = new Preferences(locale, ShortPhraseUrl, new CognitiveServicesAuthorizationProvider(subscriptionKey));

            // Create a a speech client
            using (var speechClient = new SpeechClient(preferences))
            {
                //speechClient.SubscribeToPartialResult(this.OnPartialResult);
                speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

                // create an audio content and pass it a stream.
                using (var audio = new FileStream(HostingEnvironment.MapPath("~/Temp/test.wav"), FileMode.Open, FileAccess.Read))
                {
                    var deviceMetadata = new DeviceMetadata(DeviceType.Near, DeviceFamily.IotDevice, NetworkType.Ethernet, OsName.Windows, "1607", "Dell", "T3600");
                    var applicationMetadata = new ApplicationMetadata("SampleApp", "1.0.0");
                    var requestMetadata = new RequestMetadata(Guid.NewGuid(), deviceMetadata, applicationMetadata, "SampleAppService");
                
                    await speechClient.RecognizeAsync(new SpeechInput(audio, requestMetadata), this.cts.Token).ConfigureAwait(false);

                    return convertedText;
                }
            }
        }
        
    }
}