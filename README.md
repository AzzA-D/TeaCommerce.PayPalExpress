# TeaCommerce PayPal Express Payment Provider

This project contains a custom PayPal Express payment provider for [TeaCommerce](https://teacommerce.net/).

For example usage, see the /Views/PaymentForm.cshtml file.

## Process

The general journey is as follows:

1. Customer arrives at payment form
2. If they are paying with PayPalExpress then load in accompanying PayPal Express js
3. When the user clicks the Pay with PayPal button, it triggers PayPal to render it's modal dialog and the payment is requested
4. Once the transaction has been authorised, JavaScript is used to send the transaction details back to the server for validation
5. The server sends a request to PayPal to verify the status of the transaction
6. If the transaction is complete then a PaymentState of Captured is registered set the order, a positive response is sent back to the calling JavaScript and the customer is redirected to the Order Confirmation page

## General Notes

- PayPal was particularly fussy about the amount of money being requested. The total cost of the items in the basket had to add up to exactly the same as the amount being requested (including any discounts).
- JQuery is a dependency for the TeaCommerce.PayPalExpress.js file.

### Warning

There is a potential issue with this workflow: 

It is possible that a customer could close the browser _after_ making the PayPal payment but _before_ the transaction is sent to the server for verification. If this occurs then the customer will have paid but TeaCommerce will not be notified and the order will be stuck without having completed.

One way to get around this could be to use [PayPal Instant Payment Notifications](https://developer.paypal.com/docs/classic/products/instant-payment-notification/) to verify the payment, but ironically I found them a bit too slow in my testing (ie, _not_ instant).

YMMV but personally this has not been an issue that has warranted fixing yet.