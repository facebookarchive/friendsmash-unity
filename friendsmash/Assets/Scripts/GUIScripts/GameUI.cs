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

public class GameUI : MonoBehaviour
{
    //   UI References (Set in Unity Editor)   //
    public GameObject[] Hearts;
    public Text ScoreText;
    public Text SmashText;
    public RawImage FriendImage;

    //   Internal tracking   //
    private int lastLifeCount, lastScoreCount;

    void Start ()
    {
        ScoreText.text = "Score: " + GameStateManager.Score.ToString();
        SmashText.text = "Smash " + GameStateManager.FriendName;
        lastLifeCount = GameStateManager.StartingLives;
        lastScoreCount = GameStateManager.StartingScore;
    }

    void Update ()
    {
        // update friendImage if necessary
        if (FriendImage.texture != GameStateManager.FriendTexture)
        {
            FriendImage.texture = GameStateManager.FriendTexture;
        }

        // update score if necessary
        if (lastScoreCount != GameStateManager.Score)
        {
            lastScoreCount = GameStateManager.Score;
            ScoreText.text = "Score: " + lastScoreCount.ToString();
        }

        // update lives if necessary
        if (lastLifeCount != GameStateManager.LivesRemaining)
        {
            if (GameStateManager.LivesRemaining < 3)
            {
                Hearts[GameStateManager.LivesRemaining].SetActive(false);
            }
            lastLifeCount = GameStateManager.LivesRemaining;
        }
    }
}
