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
using UnityEngine.UI;
using System.Collections.Generic;

public class PaymentDialog : MonoBehaviour
{
    //   UI References (Set in Unity Editor)   //
    public Text CoinText;
    public Text BombText;

    //   Commerce Config   //
    private enum BombPackage { Fifteen = 15, Twenty = 20, Forty = 40 };
    private readonly Dictionary<BombPackage, int> BombPackageCost = new Dictionary<BombPackage, int>
    {
        { BombPackage.Fifteen, 5},
        { BombPackage.Twenty, 12},
        { BombPackage.Forty, 25}
    };

    //   Built-in   //
    void OnEnable ()
    {
        // When PaymentDialog is enabled, update the User Interface
        UpdateUI();
    }

    //   UI   //
    public void UpdateUI ()
    {
        //Set Coin and Bomb counters
        CoinText.text = GameStateManager.CoinBalance.ToString();
        BombText.text = GameStateManager.NumBombs.ToString();
    }

    //   Buttons   //
    public void CloseDialog ()
    {
        gameObject.SetActive(false);
    }

    //   Non-premium commerce   //
    public void PurchaseBombs (int buttonIndex)
    {
        Debug.Log("PurchaseBombs Index: "+buttonIndex.ToString());
        switch (buttonIndex)
        {
        case 0:
            OnBombBuy(BombPackage.Fifteen);
            break;
        case 1:
            OnBombBuy(BombPackage.Twenty);
            break;
        case 2:
            OnBombBuy(BombPackage.Forty);
            break;
        default:
            break;
        }
    }

    private void OnBombBuy(BombPackage bPackage)
    {
        int price = BombPackageCost[bPackage];
        if (price <= GameStateManager.CoinBalance)
        {
            // execute transaction
            GameStateManager.CoinBalance -= price;
            GameStateManager.NumBombs += (int)bPackage;
            
            // update UI
            GameStateManager.CallUIRedraw();
            PopupScript.SetPopup("Purchase Complete",1f);
            
            // log App Event for spending credits
            FBAppEvents.SpentCoins(price, bPackage.ToString());
        }
        else
        {
            PopupScript.SetPopup("Not enough coins",1f);
        }
    }

    //   Premium commerce   //
    public void PurchaseCoins (int buttonIndex)
    {
        Debug.Log("PurchaseCoins Index: "+buttonIndex.ToString());
        switch (buttonIndex)
        {
        case 0:
            FBPayments.BuyCoins(FBPayments.CoinPackage.Hundred);
            break;
        case 1:
            FBPayments.BuyCoins(FBPayments.CoinPackage.TwoFifty);
            break;
        case 2:
            FBPayments.BuyCoins(FBPayments.CoinPackage.EightHundred);
            break;
        default:
            break;
        }
    }
}
