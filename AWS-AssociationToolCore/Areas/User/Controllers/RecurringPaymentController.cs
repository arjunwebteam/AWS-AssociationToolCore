using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AWS_AssociationToolCore.Areas.User.Controllers
{
    [Area("User")]
    public class RecurringPaymentController : Controller
    {
        HomeController _HomeController = new HomeController();
        AWS_AssociationToolCore.BLL.PaymentSettings _PaymentSettings = new BLL.PaymentSettings();

        public async Task<(string status, string subscriptionId)> CreateSquareSubscriptionAsync(
            string accessToken,
            string squareEnvironment,
            string locationId,
            Entities.Members objDonors,
            string customerId,
            string cardId)
        {
            try
            {
                // ── Map RecurringType → Square cadence ───────────────────────────────
                string cadence;
                switch (objDonors.RecurringType)
                {
                    case "Weekly": cadence = "WEEKLY"; break;
                    case "Monthly": cadence = "MONTHLY"; break;
                    case "Quarterly": cadence = "NINETY_DAYS"; break;
                    case "Half Yearly": cadence = "SIXTY_DAYS"; break;
                    case "Yearly": cadence = "ANNUAL"; break;
                    default:
                        return ($"Unknown RecurringType: {objDonors.RecurringType}", "");
                }

                // ── Next billing date — InsertedTime is already snapped to Jul 1 ────
                DateTime nextBilling = objDonors.InsertedTime != DateTime.MinValue
                                           ? objDonors.InsertedTime
                                           : DateTime.UtcNow;

                long amountInCents = (long)(objDonors.MemberAmount * 100);
                string baseUrl = squareEnvironment == "production"
                                       ? "https://connect.squareup.com"
                                       : "https://connect.squareupsandbox.com";

                using (var http = new System.Net.Http.HttpClient())
                {
                    http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    http.DefaultRequestHeaders.Add("Square-Version", "2021-06-16");
                    http.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // ── STEP A: Create Catalog Plan via REST ─────────────────────────
                    string tempPlanId = $"#plan-{Guid.NewGuid():N}";

                    var catalogPayload = new
                    {
                        idempotency_key = Guid.NewGuid().ToString(),
                        @object = new
                        {
                            type = "SUBSCRIPTION_PLAN",
                            id = tempPlanId,
                            subscription_plan_data = new
                            {
                                name = $"{objDonors.MembershipType ?? "Donation"} - {objDonors.RecurringType}",
                                phases = new[]
                                {
                            new
                            {
                                cadence               = cadence,
                                recurring_price_money = new
                                {
                                    amount   = amountInCents,
                                    currency = "USD"
                                }
                            }
                        }
                            }
                        }
                    };

                    string catalogJson = Newtonsoft.Json.JsonConvert.SerializeObject(catalogPayload);
                    var catalogContent = new System.Net.Http.StringContent(
                                                 catalogJson,
                                                 System.Text.Encoding.UTF8,
                                                 "application/json");

                    var catalogHttpResponse = await http.PostAsync($"{baseUrl}/v2/catalog/object", catalogContent);
                    var catalogBody = await catalogHttpResponse.Content.ReadAsStringAsync();

                    if (!catalogHttpResponse.IsSuccessStatusCode)
                        return ($"Catalog plan creation failed: {catalogBody}", "");

                    dynamic catalogResult = Newtonsoft.Json.JsonConvert.DeserializeObject(catalogBody);
                    string planId = catalogResult.catalog_object.id?.ToString() ?? "";

                    if (string.IsNullOrEmpty(planId))
                        return ("Catalog plan creation failed: plan ID was empty", "");

                    // ── STEP B: Create Subscription using plan_id ────────────────────
                    var subscriptionPayload = new
                    {
                        idempotency_key = Guid.NewGuid().ToString(),
                        location_id = locationId,
                        customer_id = customerId,
                        card_id = cardId,
                        plan_id = planId,
                        start_date = nextBilling.ToString("yyyy-MM-dd")  // ✅ 2027-07-01
                    };

                    string subJson = Newtonsoft.Json.JsonConvert.SerializeObject(subscriptionPayload);
                    var subContent = new System.Net.Http.StringContent(
                                             subJson,
                                             System.Text.Encoding.UTF8,
                                             "application/json");

                    var subHttpResponse = await http.PostAsync($"{baseUrl}/v2/subscriptions", subContent);
                    var subBody = await subHttpResponse.Content.ReadAsStringAsync();

                    if (!subHttpResponse.IsSuccessStatusCode)
                        return ($"Subscription failed: {subBody}", "");

                    dynamic subResult = Newtonsoft.Json.JsonConvert.DeserializeObject(subBody);
                    string subId = subResult.subscription.id?.ToString() ?? "";

                    objDonors.SubscriptionId = subId;
                    return ("Success", subId);
                }
            }
            catch (Exception ex)
            {
                return ($"Subscription failed: {ex.Message}", "");
            }
        }

    }
}
