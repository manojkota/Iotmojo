﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotMojo
{
    public class CognitiveServicesAuthorizationProvider
    {
        //    public static readonly string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";
        //    private string subscriptionKey;
        //    private string token;
        //    private Timer accessTokenRenewer;

        //    //Access token expires every 10 minutes. Renew it every 9 minutes only.
        //    private const int RefreshTokenDuration = 9;

        //    public CognitiveServicesAuthorizationProvider(string subscriptionKey)
        //    {
        //        this.subscriptionKey = subscriptionKey;
        //        this.token = FetchToken(FetchTokenUri, subscriptionKey).Result;

        //        // renew the token every specfied minutes
        //        accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
        //                                       this,
        //                                       TimeSpan.FromMinutes(RefreshTokenDuration),
        //                                       TimeSpan.FromMilliseconds(-1));
        //    }

        //    public string GetAccessToken()
        //    {
        //        return this.token;
        //    }

        //    private void RenewAccessToken()
        //    {
        //        this.token = FetchToken(FetchTokenUri, this.subscriptionKey).Result;
        //    }

        //    private void OnTokenExpiredCallback(object stateInfo)
        //    {
        //        try
        //        {
        //            RenewAccessToken();
        //        }
        //        catch (Exception ex)
        //        {
        //            //Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
        //        }
        //        finally
        //        {
        //            try
        //            {
        //                accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
        //            }
        //            catch (Exception ex)
        //            {
        //                //Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
        //            }
        //        }
        //    }

        //    private async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        //            UriBuilder uriBuilder = new UriBuilder(fetchUri);
        //            uriBuilder.Path += "/issueToken";

        //            var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
        //            return await result.Content.ReadAsStringAsync();
        //        }
        //    }
        //}

        //public class Recognize
        //{
        //    private string Uri = "https://speech.platform.bing.com/recognize";
        //    public async void Run(string accessToken)
        //    {

        //    }
        //}
    }
}
