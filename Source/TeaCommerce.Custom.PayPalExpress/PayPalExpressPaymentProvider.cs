using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Web.PaymentProviders;

namespace TeaCommerce.Custom.PayPalExpress
{
    [PaymentProvider("PayPalExpress")]
    public class PayPalExpressPaymentProvider : APaymentProvider
    {
        public override IDictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = new Dictionary<string, string>();

                settings["env"] = "sandbox";

                settings["button_style_label"] = "checkout";
                settings["button_style_size"] = "responsive";
                settings["button_style_shape"] = "pill";
                settings["button_style_color"] = "gold";

                settings["client_sandbox_id"] = string.Empty;
                settings["client_sandbox_secret"] = string.Empty;
                settings["client_production_id"] = string.Empty;
                settings["client_production_secret"] = string.Empty;

                return settings;
            }
        }

        public override PaymentHtmlForm GenerateHtmlForm(Order order, string teaCommerceContinueUrl, string teaCommerceCancelUrl,
            string teaCommerceCallBackUrl, string teaCommerceCommunicationUrl, IDictionary<string, string> settings)
        {
            var htmlForm = new PaymentHtmlForm();

            htmlForm.InputFields["return"] = teaCommerceContinueUrl;
            htmlForm.InputFields["cancel_return"] = teaCommerceCancelUrl;
            htmlForm.InputFields["notify_url"] = teaCommerceCallBackUrl;

            htmlForm.InputFields["env"] = settings["env"];

            htmlForm.InputFields["button_style_label"] = settings["button_style_label"];
            htmlForm.InputFields["button_style_size"] = settings["button_style_size"];
            htmlForm.InputFields["button_style_shape"] = settings["button_style_shape"];
            htmlForm.InputFields["button_style_color"] = settings["button_style_color"];

            htmlForm.InputFields["client_sandbox_id"] = settings["client_sandbox_id"];
            htmlForm.InputFields["client_production_id"] = settings["client_production_id"];

            return htmlForm;
        }

        public override string GetCancelUrl(Order order, IDictionary<string, string> settings)
        {
            return "";
        }

        public override string GetContinueUrl(Order order, IDictionary<string, string> settings)
        {
            return "";
        }

        public override CallbackInfo ProcessCallback(Order order, HttpRequest request, IDictionary<string, string> settings)
        {
            try
            {
                var paymentId = request.Form["transactions[0][related_resources][0][sale][id]"];

                // Verify against PayPal
                var isSandbox = settings["env"] == "sandbox";

                // Login and get an access token
                var accessToken = GetAccessToken(isSandbox, settings);

                var url = string.Format("https://{0}/v1/payments/capture/{1}",
                    isSandbox ? "api.sandbox.paypal.com" : "api.paypal.com",
                    paymentId);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var jsonString = client.GetStringAsync(url).Result;
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);

                    // Make sure that PayPal agrees that this payment is "completed"
                    if (json.state == "completed")
                    {
                        return new CallbackInfo(order.TotalPrice.Value.Value, paymentId, PaymentState.Captured);
                    }
                }

                // Otherwise just break...
                throw new Exception("Payment has not been authorised");
            }
            catch (Exception ex)
            {
                // TODO - Ensure log4net is included here and this is uncommented for debugging purposes
                //LogManager.GetCurrentLoggers().First().Error("PAYPAL EXPRESS CALLBACK", ex);
                throw ex;
            }
        }

        private string GetAccessToken(bool isSandbox, IDictionary<string, string> settings)
        {
            // TODO - This should definitely use cache instead of a static helper class
            var token = Helper.PayPalExpressAccessToken;
            if (token == null || token.Item1 < DateTime.Now)
            {
                var url = string.Format("https://{0}/v1/oauth2/token",
                                        isSandbox ? "api.sandbox.paypal.com" : "api.paypal.com");

                var clientId = isSandbox ? settings["client_sandbox_id"] : settings["client_production_id"];
                var clientSecret = isSandbox ? settings["client_sandbox_secret"] : settings["client_production_secret"];

                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.UTF8.GetBytes(clientId + ":" + clientSecret);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    client.DefaultRequestHeaders.IfModifiedSince = DateTime.UtcNow;

                    var requestParams = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("grant_type", "client_credentials")
                            };

                    var content = new FormUrlEncodedContent(requestParams);
                    var webresponse = client.PostAsync(url, content).Result;
                    var jsonString = webresponse.Content.ReadAsStringAsync().Result;

                    // TODO - Ensure log4net is included here and this is uncommented for debugging purposes
                    // LogManager.GetCurrentLoggers().First().Info("PAYPAL EXPRESS ACCESS KEY VALUE: " + Environment.NewLine + jsonString);

                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                    var accessToken = (string)json.access_token;
                    var expiresIn = (int)json.expires_in;
                    var expiryDate = DateTime.Now.AddSeconds(expiresIn);

                    token = new Tuple<DateTime, string>(expiryDate, accessToken);
                    Helper.PayPalExpressAccessToken = token;
                }
            }
            return token.Item2;
        }
    }
}