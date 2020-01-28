using System;
using System.Globalization;
using System.Linq;
using TeaCommerce.Api.Models;
using TeaCommerce.Umbraco.Web;

namespace TeaCommerce.Custom.PayPalExpress
{
    public static class Helper
    {
        public static int StoreId = 1;

        public static Tuple<DateTime, string> PayPalExpressAccessToken;

        public static string GetPayPalExpressTransactionsAsJsonString(this Order order)
        {
            var items = order.OrderLines.Select(o => new
            {
                name = o.Name,
                quantity = Convert.ToInt32(o.Quantity),
                price = o.UnitPrice.Value.Value.ToString("0.00", CultureInfo.InvariantCulture),
                currency = "GBP"
            }).ToList();

            var discountAmount = 0.00m;

            if (order.DiscountCodes.Count > 0 || order.TotalPrice.Discounts.Count > 0 || order.GiftCards.Count > 0)
            {
                discountAmount = (order.TotalPrice.GiftCardsAmount.Value + order.TotalPrice.TotalDiscount.WithVat);
                items.Add(new
                {
                    name = "Discount",
                    quantity = 1,
                    price = "-" + discountAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    currency = "GBP"
                });
            }

            var countries = TC.GetCountries(Helper.StoreId);

            var transaction = new
            {
                amount = new
                {
                    total = (order.TotalPrice.WithoutDiscounts.Value - discountAmount).ToString("0.00", CultureInfo.InvariantCulture),
                    currency = "GBP",
                    details = new
                    {
                        subtotal = (order.SubtotalPrice.WithoutDiscounts.Value - discountAmount).ToString("0.00", CultureInfo.InvariantCulture),
                        shipping = order.ShipmentInformation.TotalPrice.Value.Value.ToString("0.00", CultureInfo.InvariantCulture)
                    }
                },
                invoice_number = order.CartNumber,
                item_list = new
                {
                    items = items,
                    shipping_address = new
                    {
                        recipient_name = order.Properties["shipping_firstname"] + " " + order.Properties["shipping_lastname"],
                        line1 = order.Properties["shipping_addressline1"],
                        line2 = order.Properties["shipping_addressline2"],
                        city = order.Properties["shipping_city"],
                        // PayPal doesn't like UK as a country code - prefers GB
                        country_code = countries.First(c => c.Id.ToString() == order.ShipmentInformation.CountryId.ToString()).RegionCode == "UK" ? "GB" : countries.First(c => c.Id.ToString() == order.ShipmentInformation.CountryId.ToString()).RegionCode,
                        postal_code = order.Properties["shipping_postcode"],
                    }
                }
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(transaction);
        }
    }
}
