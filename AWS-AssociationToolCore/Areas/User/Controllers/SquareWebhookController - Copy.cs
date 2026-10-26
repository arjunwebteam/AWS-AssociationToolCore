//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Threading.Tasks;

//namespace AWS_AssociationToolCore.Areas.User.Controllers
//{
//    [Area("User")]
//    [Route("api/square-webhook")]
//    public class SquareWebhookController : Controller
//    {
//        AWS_AssociationToolCore.BLL.Members _Members = new BLL.Members();
//        AWS_AssociationToolCore.BLL.Users _Users = new BLL.Users();

//         ================================================================
//         RECEIVE WEBHOOK FROM SQUARE
//         ================================================================
//        [HttpPost]
//        [IgnoreAntiforgeryToken]
//        public async Task<IActionResult> Receive()
//        {
//            Int64 LogId = 0;
//            try
//            {
//                var objLogReport = new Entities.ApplicationLogs
//                {
//                    LogTitle = "Square Webhook",
//                    LogDescription = "Square Webhook Received",
//                    LogDate = DateTime.Now,
//                    InsertedBy = "Webhook",
//                    InsertedDate = DateTime.Now,
//                    UpdatedBy = "Webhook",
//                    UpdatedDate = AWS_AssociationToolCore.BLL.Common.GetTimeZoneAdjustedDateTime()
//                };
//                _Users.LogReportInsert(objLogReport, ref LogId);
//            }
//            catch { LogId = 0; }

//            string body = "";
//            try
//            {
//                using (var reader = new System.IO.StreamReader(Request.Body))
//                    body = await reader.ReadToEndAsync();

//                SafeLog(LogId, "Square Webhook", "Raw Webhook Received: " + body);

//                if (string.IsNullOrEmpty(body))
//                {
//                    SafeLog(LogId, "Square Webhook", "❌ Empty body received. Exiting.");
//                    return Ok();
//                }

//                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(body);
//                string eventType = json?.type?.ToString() ?? "";

//                SafeLog(LogId, "Square Webhook", $"EventType: {eventType}");

//                switch (eventType)
//                {
//                    case "invoice.payment_made":
//                        await HandleInvoicePaymentMade(json, LogId);
//                        break;
//                    case "subscription.updated":
//                        await HandleSubscriptionUpdated(json, LogId);
//                        break;
//                    case "payment.completed":
//                        await HandlePaymentCompleted(json, LogId);
//                        break;
//                    default:
//                        SafeLog(LogId, "Square Webhook",
//                            $"⚠️ Unhandled EventType: {eventType}");
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                SafeLog(LogId, "Square Webhook ERROR",
//                    $"Receive() Exception: {ex.Message}");
//            }

//            return Ok();
//        }

//         ================================================================
//         HANDLE: invoice.payment_made
//         ================================================================
//        private async Task HandleInvoicePaymentMade(dynamic json, long LogId)
//        {
//            try
//            {
//                 ── Read all fields ──────────────────────────────────────
//                string transactionId = json?.data?.object_?.invoice
//                                           ?.id?.ToString() ?? "";
//                string status = json?.data?.object_?.invoice
//                                           ?.status?.ToString() ?? "";
//                string orderId = json?.data?.object_?.invoice
//                                           ?.order_id?.ToString() ?? "";

//                 ✅ CustomerId — this is the key to find member
//                 Square invoice links to subscription via customer_id
//                 Your DB: ProfileId = Square CustomerId
//                string customerId = json?.data?.object_?.invoice
//                                        ?.primary_recipient
//                                        ?.customer_id?.ToString() ?? "";

//                 ✅ subscriptionId — try invoice root first
//                string subscriptionId = json?.data?.object_?.invoice
//                                            ?.subscription_id?.ToString() ?? "";

//                 ✅ fallback: try payment_requests[0]
//                if (string.IsNullOrEmpty(subscriptionId))
//                {
//                    subscriptionId = json?.data?.object_?.invoice
//                                         ?.payment_requests?[0]
//                                         ?.subscription_id?.ToString() ?? "";
//                }

//                 ✅ Read amount and payment source
//                decimal amount = 0;
//                string autoPaySource = "";
//                string cardId = "";

//                var paymentRequest = json?.data?.object_?.invoice
//                                         ?.payment_requests?[0];
//                if (paymentRequest != null)
//                {
//                    var amountMoney = paymentRequest?.total_completed_amount_money;
//                    if (amountMoney != null)
//                    {
//                        long cents = (long)(amountMoney.amount ?? 0);
//                        amount = cents / 100m;
//                    }
//                    autoPaySource = paymentRequest?.automatic_payment_source
//                                                  ?.ToString() ?? "";
//                    cardId = paymentRequest?.card_id?.ToString() ?? "";
//                }

//                SafeLog(LogId, "Square Webhook",
//                    $"invoice.payment_made | SubId:{subscriptionId} | CustomerId:{customerId} | Status:{status} | Amount:{amount} | TxnId:{transactionId} | OrderId:{orderId} | AutoSource:{autoPaySource} | CardId:{cardId}");

//                 ✅ Must be CARD_ON_FILE = subscription auto - debit
//                if (autoPaySource != "CARD_ON_FILE")
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"ℹ️ Not a subscription auto-debit. AutoSource:{autoPaySource} — Skipping.");
//                    return;
//                }

//                 ── Find member ──────────────────────────────────────────
//                int refStatus = 0;
//                Entities.MemberSubscriptions objSub = null;

//                 ✅ Try subscriptionId first
//                if (!string.IsNullOrEmpty(subscriptionId))
//                {
//                    objSub = _Members.GetMemberSubscriptionBySubId(
//                        subscriptionId, ref refStatus);
//                    SafeLog(LogId, "Square Webhook",
//                        $"GetMemberSubscriptionBySubId Result:{refStatus} | SubId:{subscriptionId}");
//                }

//                 ✅ Fallback: find by customerId = ProfileId in your DB
//                if (objSub == null && !string.IsNullOrEmpty(customerId))
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"Trying ProfileId lookup | CustomerId:{customerId}");
//                    objSub = _Members.GetMemberSubscriptionByProfileId(
//                        customerId, ref refStatus);
//                    SafeLog(LogId, "Square Webhook",
//                        $"GetMemberSubscriptionByProfileId Result:{refStatus} | CustomerId:{customerId}");
//                }

//                if (objSub == null)
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"❌ No member found | SubId:{subscriptionId} | CustomerId:{customerId}");
//                    return;
//                }

//                SafeLog(LogId, "Square Webhook",
//                    $"✅ Member found | MemberId:{objSub.MemberId} | RecurringStartTime:{objSub.RecurringStartTime}");

//                 ── Calculate ExpiryDate ─────────────────────────────────
//                 Use RecurringStartTime from DB — NOT DateTime.Now
//                 Avoids wrong date if webhook arrives slightly late
//                DateTime fromDate = objSub.RecurringStartTime != DateOnly.MinValue
//                    ? objSub.RecurringStartTime.ToDateTime(TimeOnly.MinValue)
//                    : DateTime.Now;

//                var (nextExpiry, autoDebitDate) = GetNextExpiryDate(fromDate);

//                SafeLog(LogId, "Square Webhook",
//                    $"ExpiryDate:{nextExpiry:yyyy-MM-dd} | AutoDebitDate:{autoDebitDate:yyyy-MM-dd}");

//                 ── Insert new MembershipOrder ───────────────────────────
//                 ── Update ExpiryDate only — no new INSERT ──
//                Int64 orderResult = _Members.UpdateMemberOrderExpiryBySubId(
//                    new Entities.MembershipOrders
//                    {
//                        MemberId = objSub.MemberId,
//                        ExpiryDate = nextExpiry      // ✅ 2027-06-30
//                                                      ✅ No TransactionId,
//                        Amount,
//                        PaymentStatusId etc needed
//                    });

//                SafeLog(LogId, "Square Webhook",
//                    $"UpdateMemberOrderExpiryBySubId Result:{orderResult} | MemberId:{objSub.MemberId} | ExpiryDate:{nextExpiry:yyyy-MM-dd}");

//                 ── Update RecurringStartTime ────────────────────────────
//                Int64 subNextResult = _Members.UpdateSubscriptionNextDate(
//                    new Entities.MemberSubscriptions
//                    {
//                        MemberId = objSub.MemberId,
//                        SubscriptionId = objSub.SubscriptionId,
//                        RecurringStartTime = DateOnly.FromDateTime(autoDebitDate)
//                    });

//                SafeLog(LogId, "Square Webhook",
//                    $"UpdateSubscriptionNextDate Result:{subNextResult}");

//                SafeLog(LogId, "Square Webhook",
//                    $"✅ FINAL SUCCESS | MemberId:{objSub.MemberId} | ExpiryDate:{nextExpiry:yyyy-MM-dd} | NextAutoDebit:{autoDebitDate:yyyy-MM-dd}");
//                }
//                catch (Exception ex)
//                {
//                    SafeLog(LogId, "Square Webhook ERROR",
//                        $"HandleInvoicePaymentMade Exception: {ex.Message}");
//                }
//                }

//         ================================================================
//         HANDLE: subscription.updated
//         Square fires when status changes(ACTIVE, CANCELED, DEACTIVATED)
//         ================================================================
//        private async Task HandleSubscriptionUpdated(dynamic json, long LogId)
//        {
//            try
//            {
//                 ✅ subscription_id IS available here at correct path
//                string subscriptionId = json?.data?.object_?.subscription
//                                            ?.id?.ToString() ?? "";
//                string subStatus = json?.data?.object_?.subscription
//                                            ?.status?.ToString() ?? "";
//                string customerId = json?.data?.object_?.subscription
//                                            ?.customer_id?.ToString() ?? "";

//                SafeLog(LogId, "Square Webhook",
//                    $"subscription.updated | SubId:{subscriptionId} | Status:{subStatus} | CustomerId:{customerId}");

//                if (string.IsNullOrEmpty(subscriptionId))
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        "❌ subscriptionId is empty. Exiting.");
//                    return;
//                }

//                int refStatus = 0;
//                var objSub = _Members.GetMemberSubscriptionBySubId(
//                    subscriptionId, ref refStatus);

//                 ✅ Fallback by customerId
//                if (objSub == null && !string.IsNullOrEmpty(customerId))
//                {
//                    objSub = _Members.GetMemberSubscriptionByProfileId(
//                        customerId, ref refStatus);
//                }

//                if (objSub == null)
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"❌ No member found | SubId:{subscriptionId}");
//                    return;
//                }

//                if (subStatus == "CANCELED" || subStatus == "DEACTIVATED")
//                {
//                    Int64 statusResult = _Members.UpdateSubscriptionStatus(
//                        new Entities.MemberSubscriptions
//                        {
//                            MemberId = objSub.MemberId,
//                            SubscriptionId = subscriptionId,
//                            RecurringType = subStatus
//                        });

//                    SafeLog(LogId, "Square Webhook",
//                        $"✅ Subscription {subStatus} | Result:{statusResult} | MemberId:{objSub.MemberId}");
//                }
//                else
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"ℹ️ Status '{subStatus}' — no DB action needed.");
//                }
//            }
//            catch (Exception ex)
//            {
//                SafeLog(LogId, "Square Webhook ERROR",
//                    $"HandleSubscriptionUpdated Exception: {ex.Message}");
//            }
//        }

//         ================================================================
//         HANDLE: payment.completed
//         ✅ Skip if subscriptionId exists — invoice.payment_made handles it
//         Only handle one-time payments here
//         ================================================================
//        private async Task HandlePaymentCompleted(dynamic json, long LogId)
//        {
//            try
//            {
//                string paymentId = json?.data?.object_?.payment
//                                             ?.id?.ToString() ?? "";
//                string status = json?.data?.object_?.payment
//                                             ?.status?.ToString() ?? "";
//                string subscriptionId = json?.data?.object_?.payment
//                                             ?.subscription_id?.ToString() ?? "";
//                decimal amount = 0;

//                var amountMoney = json?.data?.object_?.payment?.amount_money;
//                if (amountMoney != null)
//                {
//                    long cents = (long)(amountMoney.amount ?? 0);
//                    amount = cents / 100m;
//                }

//                SafeLog(LogId, "Square Webhook",
//                    $"payment.completed | PaymentId:{paymentId} | Status:{status} | SubId:{subscriptionId} | Amount:{amount}");

//                 ✅ Subscription payment already handled by invoice.payment_made
//                 Skip to avoid duplicate MembershipOrders rows
//                if (!string.IsNullOrEmpty(subscriptionId))
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"ℹ️ Subscription payment — invoice.payment_made handled it. Skipping.");
//                    return;
//                }

//                 ✅ One - time payment only
//                if (status != "COMPLETED")
//                {
//                    SafeLog(LogId, "Square Webhook",
//                        $"❌ Status:{status} not COMPLETED. Exiting.");
//                    return;
//                }

//                SafeLog(LogId, "Square Webhook",
//                    $"ℹ️ One-time payment | PaymentId:{paymentId} | Amount:{amount}");
//            }
//            catch (Exception ex)
//            {
//                SafeLog(LogId, "Square Webhook ERROR",
//                    $"HandlePaymentCompleted Exception: {ex.Message}");
//            }
//        }

//         ================================================================
//         HELPER: Calculate ExpiryDate AND AutoDebitDate


//         Jun 30   → year+1 July 1 → expiry year+1 June 30
//         Jan–Mar  → same year July 1 → expiry same year June 30
//         Apr–Dec  → next year July 1 → expiry next year June 30
//         ================================================================
//        private (DateTime expiryDate, DateTime autoDebitDate) GetNextExpiryDate(
//            DateTime fromDate)
//        {
//            DateTime nextJuly1;

//            if (fromDate.Month == 6 && fromDate.Day == 30)
//                 Payment ON June 30 → next = year+1 July 1
//                nextJuly1 = new DateTime(fromDate.Year + 1, 7, 1);
//            else if (fromDate.Month < 4)
//                 Jan, Feb, Mar → same year July 1
//                nextJuly1 = new DateTime(fromDate.Year, 7, 1);
//            else
//                 Apr to Dec → next year July 1
//                nextJuly1 = new DateTime(fromDate.Year + 1, 7, 1);

//             ExpiryDate    = June 30 (1 day before July 1)
//             AutoDebitDate = June 30 (Square charges on this day)
//            DateTime expiryDate = nextJuly1.AddDays(-1);
//            DateTime autoDebitDate = expiryDate;

//            return (expiryDate, autoDebitDate);
//        }

//         ================================================================
//         SAFE LOG HELPER
//         ================================================================
//        private void SafeLog(long LogId, string title, string description)
//        {
//            try
//            {
//                _Users.LogSubReportInsert(new Entities.Logdetails
//                {
//                    LogId = LogId,
//                    LogTitle = title,
//                    LogDescription = description,
//                    LogDate = DateTime.Now,
//                    InsertedBy = "Webhook",
//                    InsertedDate = DateTime.Now,
//                    UpdatedBy = "Webhook",
//                    UpdatedDate = AWS_AssociationToolCore.BLL.Common
//                                        .GetTimeZoneAdjustedDateTime()
//                });
//            }
//            catch { }
//        }
//    }
//}