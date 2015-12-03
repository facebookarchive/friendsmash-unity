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

public class FriendMarker : MonoBehaviour
{
    // Set in Unity Editor
    public Texture FriendTexture, EnemyTexture; //defaults (blue, red) set in Unity Editor
    public float FriendThreshold = 0.5f;

    private Texture[] CelebTextures;

    void Start()
    {
        Material mat = gameObject.GetComponent<MeshRenderer>().material;

        //Get celebrity resources
        GameObject gResources = GameObject.FindGameObjectWithTag("GameResources");
        CelebTextures = (gResources) ? gResources.GetComponent<GameResources>().CelebTextures : null;

        // Roll to spawn a Friend or an Enemy
        float diceRoll = Random.value;
        if (diceRoll <= FriendThreshold)
        {
            gameObject.tag = "Friend";
            // Check to see if we have a celebFriend
            if (GameStateManager.CelebFriend != -1 )
            {
                FriendTexture = CelebTextures[GameStateManager.CelebFriend];
            }
            else if (GameStateManager.FriendTexture != null)
            {
                FriendTexture = GameStateManager.FriendTexture;
            }
            // Set friend texture
            mat.mainTexture = FriendTexture;
        }
        else
        {
            gameObject.tag = "Enemy";
            if (CelebTextures != null)
            {
                // Choose a random celebrity
                int which = Random.Range(1, CelebTextures.Length - 1);
                if (GameStateManager.CelebFriend == which)
                {
                    which = CelebTextures.Length - 1;
                }
                EnemyTexture = CelebTextures[which];
            }
            // Set enemy texture
            mat.mainTexture = EnemyTexture;
        }
    }
}
