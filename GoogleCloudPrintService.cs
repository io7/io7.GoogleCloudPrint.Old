using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using GoogleCloudPrint.Model;

namespace GoogleCloudPrint
{
    public class GoogleCloudPrintService
    {
        private readonly string _source;
        private string _serviceAccountEmail;
        private readonly string _keyFilePath;
        private readonly string _keyFileSecret;
        private readonly ServiceAccountCredential _credentials;

        public List<CloudPrinter> Printers = new List<CloudPrinter>();

        public GoogleCloudPrintService(string serviceAccountEmail, string keyFilePath, string keyFileSecret, string source)
        {
            _serviceAccountEmail = serviceAccountEmail;
            _keyFilePath = keyFilePath;
            _keyFileSecret = keyFileSecret;
            _source = source;

            _credentials = Authorize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCloudPrintService"/> class.
        /// </summary>
        /// <param name="jsonCredencialFilePath">The json credencial file path.</param>
        /// <param name="source">The source.</param>
        public GoogleCloudPrintService(string jsonCredencialFilePath, string source)
        {
            _source = source;

            _credentials = Authorize(jsonCredencialFilePath);
        }

        public Task<CloudPrinters> GetPrintersAsync()
        {
            return Task<CloudPrinters>.Factory.StartNew(GetPrinters);
        }

        public Task<CloudPrintJob> PrintAsync(string printerId, string title, byte[] document, String mimeType)
        {
            return Task<CloudPrintJob>.Factory.StartNew(() => PrintDocument(printerId, title, document, mimeType));
        }

        public Task<CloudPrintShare> PrinterShareAsync(string printerId, string email, bool notify)
        {
            return Task<CloudPrintShare>.Factory.StartNew(() => PrinterShare(printerId, email, notify));
        }

        public Task<CloudPrintShare> PrinterUnShareAsync(string printerId, string email)
        {
            return Task<CloudPrintShare>.Factory.StartNew(() => PrinterUnShare(printerId, email));
        }

        public CloudPrintShare PrinterUnShare(string printerId, string email)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "email", Value = email, Type = PostDataParamType.Field });

                return GCPServiceCall<CloudPrintShare>("unshare", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintShare {success = false, message = ex.Message};
            }
        }

        private void RefreshAccessToken()
        {
            if (_credentials.Token.IsExpired(_credentials.Clock))
            {
                _credentials.RequestAccessTokenAsync(CancellationToken.None).Wait();
            }
        }

        public CloudPrintShare PrinterShare(string printerId, string email, bool notify)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "email", Value = email, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "role", Value = "APPENDER", Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "skip_notification", Value = notify.ToString(), Type = PostDataParamType.Field });

                return GCPServiceCall<CloudPrintShare>("share", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintShare {success = false, message = ex.Message};
            }
        }

        public CloudPrintJob PrintDocument(string printerId, string title, byte[] document, string mimeType)
        {
            var content = "data:" + mimeType + ";base64," + Convert.ToBase64String(document);

            return PrintDocument(printerId, title, content, "dataUrl");
        }

        public CloudPrintJob PrintDocument(string printerId, string title, string url)
        {
            return PrintDocument(printerId, title, url, "url");
        }

        public CloudPrintJob PrintDocument(string printerId, string title, string content, string contentType)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "capabilities", Value = "{\"capabilities\":[{}]}", Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "contentType", Value = contentType, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "title", Value = title, Type = PostDataParamType.Field });

                var contentValue = content;

                p.Parameters.Add(new PostDataParam { Name = "content", Type = PostDataParamType.Field, Value = contentValue });

                return GCPServiceCall<CloudPrintJob>("submit", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintJob {success = false, message = ex.Message};
            }
        }

        public CloudPrinters GetPrinters()
        {
            // clear internal data, will be reset if call succeeds
            Printers = new List<CloudPrinter>();

            try
            {
                var rv = GCPServiceCall<CloudPrinters>("search");
                if (rv != null)
                {
                    Printers = rv.printers;
                }

                return rv;
            }
            catch (Exception)
            {
                return new CloudPrinters { success = false, printers = new List<CloudPrinter>() };
            }
        }

        public CloudPrintJob ProcessInvite(string printerId)
        {
            try
            {
                var p = new PostData();

                p.Parameters.Add(new PostDataParam { Name = "printerid", Value = printerId, Type = PostDataParamType.Field });
                p.Parameters.Add(new PostDataParam { Name = "accept", Value = "true", Type = PostDataParamType.Field });


                return GCPServiceCall<CloudPrintJob>("processinvite", p);
            }
            catch (Exception ex)
            {
                return new CloudPrintJob { success = false, message = ex.Message };
            }
        }

        private T GCPServiceCall<T>(string restVerb, PostData p = null) where T : class
        {
            RefreshAccessToken();
            var authCode = _credentials.Token.AccessToken;

            var request = (HttpWebRequest)WebRequest.Create($"https://www.google.com/cloudprint/{restVerb}?output=json");
            request.Method = "POST";

            // Setup the web request
            request.ServicePoint.Expect100Continue = false;

            // Add the headers
            request.Headers.Add("X-CloudPrint-Proxy", _source);
            request.Headers.Add("Authorization", "OAuth " + authCode);

            if (p == null)
            {
                request.ContentLength = 0;
            }
            else
            {
                var postData = p.GetPostData();
                var data = Encoding.UTF8.GetBytes(postData);

                request.ContentType = "multipart/form-data; boundary=" + p.Boundary;

                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            // Get response
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException webEx)
            {
                var myResponse = webEx.Response as HttpWebResponse;

                if (myResponse != null)
                {
                    var exResponseStream = myResponse.GetResponseStream();

                    if (exResponseStream == null)
                    {
                        throw;
                    }

                    var strm = new StreamReader(exResponseStream, Encoding.UTF8);
                    var resp = strm.ReadToEnd();

                    throw new Exception(resp);
                }
            }

            if (response == null)
            {
                throw new Exception("Response was null!");
            }

            var responseStream = response.GetResponseStream();

            if (responseStream == null)
            {
                throw new Exception("Response stream was null!");
            }

            using (var responseStreamReader = new StreamReader(responseStream))
            {
                var responseContent = responseStreamReader.ReadToEnd();
                var serializer = new DataContractJsonSerializer(typeof(T));
                var ms = new MemoryStream(Encoding.Unicode.GetBytes(responseContent));
                var rv = serializer.ReadObject(ms) as T;

                return rv;
            }
        }

        private ServiceAccountCredential Authorize()
        {
            var certificate = new X509Certificate2(_keyFilePath, _keyFileSecret, X509KeyStorageFlags.Exportable);

            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(_serviceAccountEmail)
                {
                    Scopes = new[] { "https://www.googleapis.com/auth/cloudprint" }
                }.FromCertificate(certificate));

            credential.RequestAccessTokenAsync(CancellationToken.None).Wait();

            return credential;
        }

        /// <summary>
        /// Authorizes the specified json credential file path.
        /// </summary>
        /// <param name="jsonCredentialPath">The json credential file path.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">JSON content does not represent valid service account credentials</exception>
        private ServiceAccountCredential Authorize(string jsonCredentialPath)
        {
            string[] scopes = {"https://www.googleapis.com/auth/cloudprint"};

            using (var stream = new FileStream(jsonCredentialPath, FileMode.Open, FileAccess.Read))
            {
                var credentialParameters = NewtonsoftJsonSerializer.Instance.Deserialize<JsonCredentialParameters>(stream);

                if (credentialParameters.Type != "service_account"
                    || string.IsNullOrEmpty(credentialParameters.ClientEmail)
                    || string.IsNullOrEmpty(credentialParameters.PrivateKey))
                    throw new InvalidOperationException("JSON content does not represent valid service account credentials.");

                _serviceAccountEmail = credentialParameters.ClientEmail;

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(credentialParameters.ClientEmail)
                    {
                        Scopes = scopes
                    }.FromPrivateKey(credentialParameters.PrivateKey));

                // this does the magic for webform that need sync results and fails with async execution
                credential.RequestAccessTokenAsync(CancellationToken.None).Wait();

                return credential;
            }
        }

        internal class PostData
        {
            private const string CRLF = "\r\n";

            internal string Boundary { get; set; }

            internal List<PostDataParam> Parameters { get; set; }

            internal PostData()
            {
                // Get boundary, default is --AaB03x
                Boundary = "----CloudPrintFormBoundary-" + DateTime.UtcNow.Ticks;

                // The set of parameters
                Parameters = new List<PostDataParam>();
            }

            internal string GetPostData()
            {
                var sb = new StringBuilder();
                foreach (var p in Parameters)
                {
                    sb.Append("--" + Boundary).Append(CRLF);

                    if (p.Type == PostDataParamType.File)
                    {
                        sb.Append(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", p.Name, p.FileName)).Append(CRLF);
                        sb.Append("Content-Type: ").Append(p.FileMimeType).Append(CRLF);
                        sb.Append("Content-Transfer-Encoding: base64").Append(CRLF);
                        sb.Append("").Append(CRLF);
                        sb.Append(p.Value).Append(CRLF);
                    }
                    else
                    {
                        sb.Append(string.Format("Content-Disposition: form-data; name=\"{0}\"", p.Name)).Append(CRLF);
                        sb.Append("").Append(CRLF);
                        sb.Append(p.Value).Append(CRLF);
                    }
                }

                sb.Append("--" + Boundary + "--").Append(CRLF);

                return sb.ToString();
            }
        }

        internal enum PostDataParamType
        {
            Field,
            File
        }

        internal class PostDataParam
        {
            public string Name { get; set; }
            public string FileName { get; set; }
            public string FileMimeType { get; set; }
            public string Value { get; set; }
            public PostDataParamType Type { get; set; }

            public PostDataParam()
            {
                FileMimeType = "text/plain";
            }
        }
    }
}
