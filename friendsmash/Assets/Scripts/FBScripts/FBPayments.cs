/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using UnityEngine;
using System.Collections.Generic;
using Facebook.Unity;

// Class responsible for Facebook Payments in Friend Smash!
// For more details on Facebook Payments see: https://developers.facebook.com/docs/payments/overview
public static class FBPayments
{
    // enum of our product offerings
    public enum CoinPackage { Hundred = 100, TwoFifty = 250, EightHundred = 800 };

    // We are using Facebook payment objects with static pricing hosted on the game server
    // See: https://developers.facebook.com/docs/payments/product
    //
    // Note: In this git repo, these objects are located at X
    // Note2: Use the Open Graph Object Debugger to force scrape your open graph objects after updating: https://developers.facebook.com/tools/debug/og/object/
    //
    private static readonly string PaymentObjectURL = GameStateManager.ServerURL+"payments/{0}.php";
    private static readonly Dictionary<CoinPackage,string> PaymentObjects = new Dictionary<CoinPackage, string>
    {
        { CoinPackage.Hundred, "100coins" },
        { CoinPackage.TwoFifty, "250coins" },
        { CoinPackage.EightHundred, "800coins" }
    };

    // Prompt the user to purchase a virtual item with the Facebook Pay Dialog
    // See: https://developers.facebook.com/docs/payments/reference/paydialog
    public static void BuyCoins (CoinPackage cPackage)
    {
        // Format payment URL
        string paymentURL = string.Format(PaymentObjectURL, PaymentObjects[cPackage]);

        // https://developers.facebook.com/docs/unity/reference/current/FB.Canvas.Pay
        FB.Canvas.Pay(paymentURL,
                      "purchaseitem",
                      1,
                      null, null, null, null, null,
                      (IPayResult result) =>
        {
            Debug.Log("PayCallback");
            if (result.Error != null)
            {
                Debug.LogError(result.Error);
                return;
            }
            Debug.Log(result.RawResult);

            object payIdObj;
            if (result.ResultDictionary.TryGetValue("payment_id", out payIdObj))
            {
                string payID = payIdObj.ToString();
                Debug.Log("Payment complete");
                Debug.Log("Payment id:" + payID);

                // Verify payment before awarding item
                if (VerifyPayment(payID))
                {
                    GameStateManager.CoinBalance += (int)cPackage;
                    GameStateManager.CallUIRedraw();
                    PopupScript.SetPopup("Purchase Complete",2f);
                }
            }
            else
            {
                Debug.Log("Payment error");
            }
        });
    }

    // Verify payment with Facebook
    // See: https://developers.facebook.com/docs/payments/implementation-guide/order-fulfillment
    //
    // Reminder: It is important to do this payment verification server-to-server
    // See more: https://developers.facebook.com/docs/payments/realtimeupdates
    //
    private static bool VerifyPayment (string paymentID)
    {
        // Payment verification is not implemented in this sample game
        return true;
    }
}
