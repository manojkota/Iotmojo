using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace IotMojo
{
    public class WatsonSpeechToText
    {
        //public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        //{
        //    try
        //    {
        //        var token = "";
        //        var requestUri = @"https://stream.watsonplatform.net/speech-to-text/api/v1/recognize?watson-token=" + token;

        //        //using (var client = new HttpClient())
        //        //{
        //        //    string startActionjson = "{\"action\": \"start\", \"content-type\": \"audio/wav\", \"continuous\" : false, \"interim_results\": false}";

        //        //    //JsonSerializerSettings sett = new JsonSerializerSettings();
        //        //    //sett.NullValueHandling = NullValueHandling.Ignore;
        //        //    //var myContent = JsonConvert.SerializeObject(request, sett);
        //        //    var buffer = Encoding.UTF8.GetBytes(startActionjson);
        //        //    var byteContent = new ByteArrayContent(buffer);

        //        //    var response = await client.PostAsync(requestUri, byteContent);
        //        //}

        //        var ws = new ClientWebSocket(@"wss://stream.watsonplatform.net/speech-to-text/api/v1/recognize?watson-token=" + token);
        //        //ws.OnOpen += ws_OnOpen;
        //        //ws.OnMessage += ws_OnMessage;
        //        //ws.OnClose += ws_OnClose;
        //        //ws.OnError += ws_OnError;

        //        string startActionjson = "{\"action\": \"start\", \"content-type\": \"audio/wav\", \"continuous\" : false, \"interim_results\": false}";
        //        await ws.SendAsync(startActionjson, b =>
        //        {
        //            if (b == true)
        //            {
        //                byte[] bytes = ReadFully(audiostream);

        //                ws.SendAsync(bytes, b1 =>
        //                {
        //                    if (b1)
        //                        ws.Close();
        //                });

        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return string.Empty;
        //    }
        //}



        Uri url = new Uri("wss://stream.watsonplatform.net/speech-to-text/api/v1/recognize");
        ArraySegment<byte> openingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"start\", \"content-type\": \"audio/wav\", \"continuous\" : false, \"interim_results\": false}"
        ));
        ArraySegment<byte> closingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"stop\"}"
        ));
        string transcribeText = string.Empty;

        public async Task<string> Transcribe(Stream audioStream)
        {
            try
            {


                var ws = new ClientWebSocket();
                ws.Options.Credentials = new NetworkCredential("29ae4b38-554a-40ea-9317-e25f4ad40d8e", "43WRUvDCHF8U");
                await ws.ConnectAsync(url, CancellationToken.None);

                // send opening message and wait for initial delimeter 
                Task.WaitAll(ws.SendAsync(openingMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleResults(ws));

                // send all audio and then a closing message; simltaneously print all results until delimeter is recieved
                Task.WaitAll(SendAudio(ws, audioStream), HandleResults(ws));

                // close down the websocket
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).Wait();

                return transcribeText;
            }
            catch (Exception ex)
            {
                transcribeText = ex.Message;
                return transcribeText;
            }
        }

        public async Task SendAudio(ClientWebSocket ws, Stream audioStream)
        {
            await ws.SendAsync(new ArraySegment<byte>(ReadFully(audioStream)), WebSocketMessageType.Binary, true, CancellationToken.None);
            await ws.SendAsync(closingMessage, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // prints results until the connection closes or a delimeterMessage is recieved
        public async Task HandleResults(ClientWebSocket ws)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var segment = new ArraySegment<byte>(buffer);

                var result = await ws.ReceiveAsync(segment, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                int count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "That's too long", CancellationToken.None);
                        return;
                    }

                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await ws.ReceiveAsync(segment, CancellationToken.None);
                    count += result.Count;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, count);

                // you'll probably want to parse the JSON into a useful object here,
                // see ServiceState and IsDelimeter for a light-weight example of that.
                Console.WriteLine(message);
                transcribeText = message;
            }
        }
        

        public byte[] ReadFully(Stream input)
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

        //public void Test()
        //{
        //    var client = new RestClient("https://stream.watsonplatform.net/speech-to-text/api/v1/recognize?timestamps=true&word_alternatives_threshold=0.9&keywords=%2522colorado%2522%252C%2522tornado%2522%252C%2522tornadoes%2522&keywords_threshold=0.5&continuous=true");
        //    var request = new RestRequest(Method.POST);
            
        //    request.AddHeader("content-type", "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW");
        //    request.AddHeader("authorization", "Basic MjlhZTRiMzgtNTU0YS00MGVhLTkzMTctZTI1ZjRhZDQwZDhlOjQzV1JVdkRDSEY4VQ==");
        //    request.AddParameter("multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW", "------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"audio\"; filename=\"audio-file.flac\"\r\nContent-Type: audio/x-flac\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW--", ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);
        //}

        public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            var requestUri = @"https://stream.watsonplatform.net/speech-to-text/api/v1/recognize?continuous=true&model=en-US_NarrowbandModel";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Basic MjlhZTRiMzgtNTU0YS00MGVhLTkzMTctZTI1ZjRhZDQwZDhlOjQzV1JVdkRDSEY4VQ==");

                using (var binaryContent = new ByteArrayContent(ReadFully(audiostream)))
                {
                    binaryContent.Headers.TryAddWithoutValidation("content-type", "audio/wav");

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
    }
}
