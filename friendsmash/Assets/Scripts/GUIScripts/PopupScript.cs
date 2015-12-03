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
using System;
using System.Collections;

public class PopupScript : MonoBehaviour
{
    private const float defaultDelay = 2f;
    public Text popupText;

    // Static method to create a popup and show it
    public static void SetPopup (string message, float delay = defaultDelay, Action callback = null)
    {
        // Create popup and attach it to UI
        GameObject popupGO = Instantiate (Resources.Load ("Prefabs/GUIpopup") as GameObject);
        GameObject PopupCanvas = GameObject.Find ("PopupContainer");
        popupGO.transform.SetParent(PopupCanvas.transform);

        // Configure popup
        PopupScript pScript = popupGO.GetComponent<PopupScript>();
        pScript.ConfigurePopup(message, delay, callback);
    }

    public void ConfigurePopup (string message, float delay, Action callback)
    {
        popupText.text = message;
        StartCoroutine(ClearPopup(delay, callback));
    }

    private IEnumerator ClearPopup (float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        if (callback != null)
        {
            callback();
        }
        Destroy(gameObject);
    }
}
