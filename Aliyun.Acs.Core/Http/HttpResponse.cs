﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using Aliyun.Acs.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Aliyun.Acs.Core.Http
{
    public class HttpResponse : HttpRequest
    {

        private static int _timeout = 100000; // No effect
        private static int bufferLength = 1024;

        public int Status { get; set; }

        public HttpResponse(string strUrl)
            : base(strUrl)
        {

        }

        public HttpResponse()
        {
        }

        public new void SetContent(byte[] content, string encoding, FormatType? format)
        {
            this.Content = content;
            this.Encoding = encoding;
            this.ContentType = format;
        }

        private static void PasrseHttpResponse(HttpResponse httpResponse, HttpWebResponse httpWebResponse)
        {
            httpResponse.Content = ReadContent(httpResponse, httpWebResponse);
            httpResponse.Status = (int)httpWebResponse.StatusCode;
            httpResponse.Headers = new Dictionary<string, string>();
            httpResponse.Method = ParameterHelper.StringToMethodType(httpWebResponse.Method);

            foreach (var key in httpWebResponse.Headers.AllKeys)
            {
                httpResponse.Headers.Add(key, httpWebResponse.Headers[key]);
            }

            string type = httpResponse.Headers["Content-Type"];
            if (null != type)
            {
                httpResponse.Encoding = "UTF-8";
                string[] split = type.Split(';');
                httpResponse.ContentType = ParameterHelper.StingToFormatType(split[0].Trim());
                if (split.Length > 1 && split[1].Contains("="))
                {
                    string[] codings = split[1].Split('=');
                    httpResponse.Encoding = codings[1].Trim().ToUpper();
                }
            }
        }

        private static async Task PasrseHttpResponseAsync(HttpResponse httpResponse, HttpWebResponse httpWebResponse)
        {
            httpResponse.Content = await ReadContentAsync(httpResponse, httpWebResponse);
            httpResponse.Status = (int)httpWebResponse.StatusCode;
            httpResponse.Headers = new Dictionary<string, string>();
            httpResponse.Method = ParameterHelper.StringToMethodType(httpWebResponse.Method);

            foreach (var key in httpWebResponse.Headers.AllKeys)
            {
                httpResponse.Headers.Add(key, httpWebResponse.Headers[key]);
            }

            string type = httpResponse.Headers["Content-Type"];
            if (null != type)
            {
                httpResponse.Encoding = "UTF-8";
                string[] split = type.Split(';');
                httpResponse.ContentType = ParameterHelper.StingToFormatType(split[0].Trim());
                if (split.Length > 1 && split[1].Contains("="))
                {
                    string[] codings = split[1].Split('=');
                    httpResponse.Encoding = codings[1].Trim().ToUpper();
                }
            }
        }

        public static byte[] ReadContent(HttpResponse response, HttpWebResponse rsp)
        {
            var buffer = new byte[bufferLength];

            using (var ms = new MemoryStream())
            using (var stream = rsp.GetResponseStream())
            {
                while (true)
                {
                    int length = stream.Read(buffer, 0, bufferLength);
                    if (length == 0)
                    {
                        break;
                    }
                    ms.Write(buffer, 0, length);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var bytes = new byte[ms.Length];
                ms.Read(bytes, 0, bytes.Length);

                return bytes;
            }
        }

        public static async Task<byte[]> ReadContentAsync(HttpResponse response, HttpWebResponse rsp)
        {
            using (var ms = new MemoryStream())
            using (var stream = rsp.GetResponseStream())
            {
                await stream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                return ms.ToArray();
            }
        }

        public static HttpResponse GetResponse(HttpRequest request, int? timeout = null)
        {
            HttpWebRequest httpWebRequest = GetWebRequest(request);
            if (timeout != null)
            {
                httpWebRequest.Timeout = (int)timeout;
            }

            HttpResponse httpResponse = new HttpResponse(httpWebRequest.RequestUri.AbsoluteUri);
            HttpWebResponse httpWebResponse = null;
            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    httpWebResponse = (HttpWebResponse)ex.Response;
                }
                else
                {
                    throw ex;
                }
            }

            PasrseHttpResponse(httpResponse, httpWebResponse);
            return httpResponse;
        }

        public static async Task<HttpResponse> GetResponseAsync(HttpRequest request, int? timeout = null)
        {
            var httpWebRequest = GetWebRequest(request);
            if (timeout != null)
            {
                httpWebRequest.Timeout = (int)timeout;
            }

            var httpResponse = new HttpResponse(httpWebRequest.RequestUri.AbsoluteUri);
            HttpWebResponse httpWebResponse = null;
            try
            {
                httpWebResponse = (await httpWebRequest.GetResponseAsync()) as HttpWebResponse;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    httpWebResponse = (HttpWebResponse)ex.Response;
                }
                else
                {
                    throw ex;
                }
            }

            await PasrseHttpResponseAsync(httpResponse, httpWebResponse);
            return httpResponse;
        }

        public static HttpWebRequest GetWebRequest(HttpRequest request)
        {
            HttpWebRequest httpWebRequest = null;
            if (request.Url.Contains("https"))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                httpWebRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(request.Url));
            }
            else
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url);
            }

            //httpWebRequest.ServicePoint.Expect100Continue = false;
            httpWebRequest.Method = request.Method.ToString();
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Timeout = _timeout;

            if (request.Headers.ContainsKey("Accept"))
            {
                httpWebRequest.Accept = DictionaryUtil.Pop(request.Headers, "Accept");
            }
            if (request.Headers.ContainsKey("Date"))
            {
                httpWebRequest.Date = Convert.ToDateTime(DictionaryUtil.Pop(request.Headers, "Date"));
            }

            foreach (var header in request.Headers)
            {
                httpWebRequest.Headers.Add(header.Key, header.Value);
            }

            return httpWebRequest;
        }

        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        public bool IsSuccess()
        {
            return 200 <= this.Status && 300 > this.Status;
        }
    }
}
