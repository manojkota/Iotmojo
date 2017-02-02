using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;

namespace AudioConverter.Controllers
{
    public class ConverterController : ApiController
    {
        [HttpPost]
        public async Task<string> Post()
        {
            try
            {
                var stream = await Request.Content.ReadAsStreamAsync();

                if (stream != null)
                {
                    WaveFormat target = new WaveFormat(16000, 8, 1);
                    WaveStream wavStream = new WaveFileReader(stream);
                    WaveFormatConversionStream str = new WaveFormatConversionStream(target, wavStream);
                    WaveFileWriter.CreateWaveFile(HostingEnvironment.MapPath("~/Temp/test.wav"), str);

                    SpeechToText txt = new SpeechToText();
                    var retValue = await txt.Run(str, "en-US", "4a47c14e828f40b8801c67212766e0f5");
                    return retValue;
                }
                return "error";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
