using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Config;

public class YouTubeLiveController : MonoBehaviour {

  [SerializeField]
  Text userName;
  [SerializeField]
  Text result;
  [SerializeField]
  InputField liveIdField;

  public string clientId = "";
  public string clientSecret = "";
  private bool isCancel;

  IEnumerator Start () {
    if (liveIdField.text.Length > 0) yield return new WaitForSeconds(2.0f);
    //if (!liveIdField.text.Contains("https://www.youtube.com/watch?v=")) yield return new WaitForSeconds(1.0f);
    //string liveId = liveIdField.text.Replace("https://www.youtube.com/watch?v=", "");

    clientId = ClientData.CLIENT_ID;
    clientSecret = ClientData.CLIENT_SECRET;

    var code = "";
    LocalServer (c => code = c);

    var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code" +
      "&client_id=" + clientId +
      "&redirect_uri=" + "http://localhost:8080" +
      "&scope=" + "https://www.googleapis.com/auth/youtube.readonly" +
      "&access_type=" + "offline";
    Application.OpenURL (authUrl);
    yield return new WaitUntil (() => code != "");

    Debug.Log (code);

    var tokenUrl = "https://www.googleapis.com/oauth2/v4/token";
    var content = new Dictionary<string, string> () { { "code", code }, { "client_id", clientId }, { "client_secret", clientSecret }, { "redirect_uri", "http://localhost:8080" }, { "grant_type", "authorization_code" }, { "access_type", "offline" },
      };
    var request = UnityWebRequest.Post (tokenUrl, content);
    yield return request.SendWebRequest ();

    var json = JSON.Parse (request.downloadHandler.text);
    var token = json["access_token"].RawString ();

    Debug.Log (token);

    var url = "https://www.googleapis.com/youtube/v3/liveBroadcasts?part=snippet";
    url += "&id=" + liveIdField.text;

    var req = UnityWebRequest.Get (url);
    req.SetRequestHeader ("Authorization", "Bearer " + token);
    yield return req.SendWebRequest ();

    json = JSON.Parse (req.downloadHandler.text);
    Debug.Log(json.ToString());

    var chatId = json["items"][0]["snippet"]["liveChatId"].RawString ();

    Debug.Log (chatId);

    //string prevMsg = "";
    int prevMsgCount = 0;
    string nextPageToken = "";

    while (true) {
      var nextURL = "https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet,authorDetails";
      nextURL += "&liveChatId=" + chatId;
      nextURL += nextPageToken == "" ? "" : "&pageToken=" + nextPageToken;

      req = UnityWebRequest.Get (nextURL);
      req.SetRequestHeader ("Authorization", "Bearer " + token);
      yield return req.SendWebRequest ();

      json = JSON.Parse (req.downloadHandler.text);
      var items = json["items"];
      //if (items.Count == 75) prevMsgCount = 0;
      if (prevMsgCount == 0 && items.Count > prevMsgCount) prevMsgCount = items.Count;

      Debug.Log ("prevMsgCount: " + prevMsgCount);
      Debug.Log ("items.Count: " + items.Count);
      var count = 1;
      foreach (var item in items) {
        if (prevMsgCount < count || prevMsgCount == 0) {
          var snip = item.Value["snippet"];
          var author = item.Value["authorDetails"];
          var msg = snip["displayMessage"].RawString ();
          if (prevMsgCount < items.Count) {
            Debug.Log (author["displayName"].RawString () + ": " + msg);
            if (msg.Contains("おみくじ") || msg.Contains("omikuji")) {
              //prevMsg = msg;
              var res = GenerateOmikuji ();
              userName.text = author["displayName"].RawString ();
              result.text = res;
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            } else if (msg.Contains("おみくじら")) {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "囲碁神";
              Debug.Log (author["displayName"].RawString () + ": " + "囲碁神");
            } else if (msg.Contains("ヌベ") || msg.Contains("ぬべ") || msg.Contains("nube")) {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "ヌベキチ";
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            } else if (msg == "なめくじ") {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "なめくじ吉";
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            } else if (msg == "つめもり" || msg == "詰碁の森" || msg == "ツメモリ") {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "詰碁の森吉";
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            } else if (msg.Contains("べに")) {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "べにキチ";
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            } else if ((msg.Contains("33") && msg.Contains("4")) || (msg.Contains("３３") && msg.Contains("４"))) {
              //prevMsg = msg;
              userName.text = author["displayName"].RawString ();
              result.text = "な阪関無吉";
              Debug.Log (author["displayName"].RawString () + ": " + msg);
            }
          }
        }
        count++;
      }

      if (items.Count > prevMsgCount) {
        prevMsgCount++;
      }

      Debug.Log ("prevMsgCount: " + prevMsgCount);
      // Debug.Log (json["nextPageToken"]);
      if (items.Count == 75) {
        nextPageToken = json["nextPageToken"];
        prevMsgCount = 0;
        //prevMsg = "";
      }

      if (isCancel) break;
      yield return new WaitForSeconds (3.0f);
    }
  }

  void LocalServer (Action<string> onReceive) {
    ThreadStart start = () => {
      try {
        var listener = new HttpListener ();
        listener.Prefixes.Add ("http://*:8080/");
        listener.Start ();

        var context = listener.GetContext ();
        var req = context.Request;
        var res = context.Response;

        var re = new Regex (@"/\?code=(?<c>.*)");
        var code = re.Match (req.RawUrl).Groups["c"].ToString ();
        onReceive (code);

        res.StatusCode = 200;
        res.Close ();
      } catch (Exception e) {
        Debug.LogError (e);
      }
    };
    new Thread (start).Start ();
  }

  private string GenerateOmikuji () {
    string res = "";
    var r = UnityEngine.Random.Range (1, 101);
    if (r > 0 && r <= 5) {
      res = "超絶囲碁吉";
    } else if (r > 5 && r <= 20) {
      res = "超囲碁吉";
    } else if (r > 20 && r <= 50) {
      res = "囲碁吉";
    } else if (r > 50 && r <= 80) {
      res = "大吉";
    } else if (r > 80 && r <= 90) {
      res = "中吉";
    } else {
      res = "吉";
    }

    return res;
  }

  private void OnDestroy () {
    isCancel = true;
  }

}

public static class SimpleJsonUtility {
  public static string RawString (this JSONNode node) {
    var len = node.ToString ().Length - 2;
    return node.ToString ().Substring (1, len);
  }
}