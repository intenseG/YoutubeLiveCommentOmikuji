using UnityEngine;
using UnityEngine.UI;

public class OmikujiManager : MonoBehaviour {
  [SerializeField]
  Text userName;
  [SerializeField]
  Text result;

  [SerializeField]
  YouTubeLive.YtlController ctrl;

  string message;

  void Start() {
    Application.runInBackground = true;

    ctrl.OnMessage += msg => {
      if (msg.text == "おみくじ" || msg.text == "omikuji") {
        string sendMsg = GenerateOmikuji();
        userName.text = msg.name;
        result.text = sendMsg;
        Debug.Log(msg.name + ": " + sendMsg);
        //ctrl.SendComment(sendMsg, msg.name);
      } else if (msg.text == "おみくじら") {
        userName.text = msg.name;
        result.text = "囲碁神";
        Debug.Log(msg.name + ": " + "囲碁神");
      }
    };
  }

  private string GenerateOmikuji() {
    string res = "";
    var r = UnityEngine.Random.Range(1, 101);
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
}