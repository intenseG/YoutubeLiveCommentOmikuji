using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace YouTubeLive {
  [RequireComponent(typeof(YtlClient))]
  [RequireComponent(typeof(YtlServer))]
  public class YtlController : MonoBehaviour {
    [SerializeField]
    Access access;

    [SerializeField]
    float interval = 3f;

    YtlClient client;
    public event Action<Chat.Msg> OnMessage;

    IEnumerator Start() {
      OnMessage += _ => {};

      client = GetComponent<YtlClient> ();
      client.access = access;
      yield return null;

      if (access.code == "") {
        var server = GetComponent<YtlServer>();

        server.Listen();
        server.OnReceiveCode += code => {
          access.code = code;
          Debug.Log("Access Code 1: " + access.code);
        };
        yield return null;

        Debug.Log("Access Code 2: " + access.code);
        Application.OpenURL (client.AuthUrl());
        yield return new WaitUntil (() => access.code != "");
        server.Stop ();
      }

      if (access.token == "") {
        Debug.Log("Start GetToken");
        client.GetToken((token,_) => {
          access.token = token;
          Debug.Log("Token: " + token);
        }, err => {
          Debug.Log("GetToken>" + err);
        });
        yield return new WaitUntil (() => access.token != "");
      }

      var chatId = "";
      client.GetLiveChatId (access.id, c => {
        chatId = c;
        Debug.Log("LiveChatId: " + c);
      }, err => {
        Debug.Log("GetLiveChatId>" + err);
      });
      yield return new WaitUntil (() => chatId != "");

      var pageToken = "";
      while (true) {
        client.GetChatMessages(chatId, pageToken, chat => {
          foreach (var msg in chat.msgs) {
            OnMessage(msg);
          }
          pageToken = chat.pageToken;
        }, err => Debug.Log("GetChatMessages>" + err));

        yield return new WaitForSeconds (interval);
      }
    }

    // public void SendComment(string msg, string name) {
    //     client.SendChatMessages(msg, name, null, null);
    // }

  }

}