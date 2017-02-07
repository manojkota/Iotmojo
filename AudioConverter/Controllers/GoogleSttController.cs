using CUETools.Codecs;
using CUETools.Codecs.FLAKE;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;

namespace AudioConverter.Controllers
{
    public class GoogleSttController : ApiController
    {
        [HttpPost]
        public async Task<string> Post()
        {
            try
            {
                var stream = await Request.Content.ReadAsStreamAsync();

                if (stream != null)
                {
                    var outStream = new MemoryStream();

                    WaveFormat target = new WaveFormat(16000, 16, 1);
                    WaveStream wavStream = new WaveFileReader(stream);
                    WaveFormatConversionStream str = new WaveFormatConversionStream(target, wavStream);
                    WaveFileWriter.CreateWaveFile(HostingEnvironment.MapPath("~/Temp/test.wav"), str);

                    using (var audio = new FileStream(HostingEnvironment.MapPath("~/Temp/test.wav"), FileMode.Open, FileAccess.Read))
                    {
                        ConvertToFlac(audio, outStream);

                        outStream.Position = 0;

                        GoogleCognitiveSpeechService txt = new GoogleCognitiveSpeechService();
                        var retValue = await txt.GetTextFromAudioAsync(outStream);
                        return retValue;
                    }
                }
                return "error";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void ConvertToFlac(Stream sourceStream, Stream destinationStream)
        {
            var audioSource = new WAVReader(null, sourceStream);
            try
            {
                if (audioSource.PCM.SampleRate != 16000)
                {
                    throw new InvalidOperationException("Incorrect frequency - WAV file must be at 16 KHz.");
                }
                var buff = new AudioBuffer(audioSource, 0x10000);
                var flakeWriter = new FlakeWriter(null, destinationStream, audioSource.PCM);
                flakeWriter.CompressionLevel = 8;
                while (audioSource.Read(buff, -1) != 0)
                {
                    flakeWriter.Write(buff);
                }
                //flakeWriter.Close();
            }
            finally
            {
                audioSource.Close();
            }
        }
    }
}
