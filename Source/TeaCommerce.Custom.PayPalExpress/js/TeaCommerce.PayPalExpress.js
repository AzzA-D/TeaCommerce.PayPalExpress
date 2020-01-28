(function () {

    $.PaypalExpress = function (buttonSelector, transactions) {

        paypal.Button.render({

            // Set your environment
            env: $("#env").val(), // sandbox | production

            // Specify the style of the button
            style: {
                label: $("#button_style_label").val(), // checkout | credit | pay
                size: $("#button_style_size").val(),    // small | medium | responsive
                shape: $("#button_style_shape").val(),     // pill | rect
                color: $("#button_style_color").val()      // gold | blue | silver
            },

            // Api details
            client: {
                sandbox: $("#client_sandbox_id").val(),
                production: $("#client_production_id").val()
            },

            commit: true,   // Show a 'Pay Now' button

            // Handle a payment
            payment: function (actions) {
                return actions.payment.create({
                    transactions: transactions
                });
            },

            // Once the payment has been authorized
            onAuthorize: function (data, actions) {
                console.log("data", data);

                return actions.payment.execute().then(function (payment) {

                    console.log("payment", payment);

                    // TODO - trigger some loading animation here while we verify payment on the server
                    console.log("checking server to verify payment");

                    // Send data to server for validation and to update TeaCommerce
                    var callbackUrl = $("#notify_url").val();
                    $.ajax({
                        type: "POST",
                        data: payment,
                        url: callbackUrl,
                        success: function (data) {
                            // Update the page with a link to the transaction completed page
                            $(".payment-button").html("<div><a href='/transactioncompleted/'>Please click here to view your order confirmation if you aren't redirected within 5 seconds</a></div>");

                            // Try to redirect the user
                            window.location.replace("/thanks-for-your-order/");
                        }
                    });

                });

            }
        }, buttonSelector);

    };

})();
