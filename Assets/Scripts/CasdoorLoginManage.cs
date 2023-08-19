using System.Collections;
using Casdoor.Client;
using System.Net.Http;
using UnityEngine;
using Microsoft.IdentityModel.Logging;
using System.Threading.Tasks;
using IdentityModel.Client;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class CasdoorLoginManage : MonoBehaviour
{

    private HttpClient httpClient;
    private CasdoorClient client;
    private TokenResponse token;
    public static CasdoorUser user;
    public static TextMeshProUGUI userInfoText;
    public static RawImage userAvatarImage;
    private GameObject inputPanel;
    private GameObject loginPanel;

    private TMP_InputField accountInput;
    private TMP_InputField passwordInput;

    public static string userInfo;
    public static string avatarUrl;

    public string Url = "";
    WebViewObject webViewObject;
    string callbackUrl = "";
    string code = "";
    string callbackBase = "";


    // Start is called before the first frame update
    private IEnumerator Start()
    {
        httpClient = new HttpClient();
        var options = new CasdoorOptions
        {
            // Require: Basic options
            Endpoint = "https://door.casdoor.com",
            OrganizationName = "casbin",
            ApplicationName = "app-example",
            ApplicationType = "native", // webapp, webapi or native
            ClientId = "b800a86702dd4d29ec4d",
            ClientSecret = "1219843a8db4695155699be3a67f10796f2ec1d5",

            // Optional: The callback path that the client will be redirected to
            // after the user has authenticated. default is "/casdoor/signin-callback"
            CallbackPath = "/callback",
            // Optional: Whether require https for casdoor endpoint
            RequireHttpsMetadata = true,
            // Optional: The scopes that the client is requesting.
            Scope = "openid profile email"

            // More options can be found at README.md
            // https://github.com/casdoor/casdoor-dotnet-sdk/blob/master/README.md
        };

        //https://door.casdoor.com/login/oauth/authorize?client_id=b800a86702dd4d29ec4d&response_type=code&redirect_uri=http://localhost:5000/callback&scope=read&state=app-example
        Url = $"{options.Endpoint.TrimEnd('/')}/login/oauth/authorize" +
              $"?client_id={options.ClientId}" +
              $"&response_type=code" +
              $"&redirect_uri=http://localhost:5000{options.CallbackPath}" +
              $"&scope={options.Scope}" +
              $"&state={options.ApplicationName}";

        client = new CasdoorClient(httpClient, options);

        // If you want look PII in logs or exception, you can set the following
        IdentityModelEventSource.ShowPII = true;

        if(userAvatarImage == null)
        {
            userAvatarImage = GameObject.Find("Canvas/Panel/BackGroundImage/UserAvatarImage").GetComponent<RawImage>();
        }

        if(inputPanel == null)
        {
            inputPanel = GameObject.Find("Canvas/Panel/BackGroundImage/InputPanel");
        }

        if (loginPanel == null)
        {
            loginPanel = GameObject.Find("Canvas/Panel/BackGroundImage/LoginPanel");
        }

        ToggleUserAvatarVisibility(false);
        InputVisibility(true);
        LoginVisibility(true);


        GameObject accountInputGO = GameObject.Find("Canvas/Panel/BackGroundImage/InputPanel/Account");
        if (accountInputGO != null)
        {
            accountInput = accountInputGO.GetComponent<TMP_InputField>();
        }
        else
        {
            Debug.LogError("Account Input Field not found!");
        }

        GameObject passwordInputGO = GameObject.Find("Canvas/Panel/BackGroundImage/InputPanel/Password");
        if (passwordInputGO != null)
        {
            passwordInput = passwordInputGO.GetComponent<TMP_InputField>();
        }
        else
        {
            Debug.LogError("Password Input Field not found!");
        }

        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();

        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));

            },
            err: (msg) =>
            {
                Debug.LogError(string.Format("CallOnError[{0}]", msg));
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
                callbackUrl = msg;
                code = System.Web.HttpUtility.ParseQueryString(new Uri(callbackUrl).Query).Get("code");

                // Inject JavaScript code to apply CSS scaling
                webViewObject.EvaluateJS(@"
                    var style = document.createElement('style');
                    style.innerHTML = `
                        body {
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            transform-origin: center center;
                            transform: scale(2.5);
                        }`;  // Adjust the scaling as needed
                    document.head.appendChild(style);
                ");


#if !UNITY_ANDROID
                // NOTE: depending on the situation, you might prefer
                // the 'iframe' approach.
                // cf. https://github.com/gree/unity-webview/issues/189
#if true
                webViewObject.EvaluateJS(@"
                    if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                        window.Unity = {
                            call: function(msg) {
                                window.webkit.messageHandlers.unityControl.postMessage(msg);
                            }
                        }
                    } else {
                        window.Unity = {
                            call: function(msg) {
                                window.location = 'unity:' + msg;
                            }
                        }
                    }
                ");
#else
                webViewObject.EvaluateJS(@"
                    if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                        window.Unity = {
                            call: function(msg) {
                                window.webkit.messageHandlers.unityControl.postMessage(msg);
                            }
                        }
                    } else {
                        window.Unity = {
                            call: function(msg) {
                                var iframe = document.createElement('IFRAME');
                                iframe.setAttribute('src', 'unity:' + msg);
                                document.documentElement.appendChild(iframe);
                                iframe.parentNode.removeChild(iframe);
                                iframe = null;
                            }
                        }
                    }
                ");
#endif
#endif
                webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
            },
            //ua: "custom user agent string",

            enableWKWebView: true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
#endif
        
        webViewObject.SetMargins(5, 5, 5, 5);

#if !UNITY_WEBPLAYER
        if (Url.StartsWith("http"))
        {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        }
        else
        {
            var exts = new string[]{
                ".jpg",
                ".js",
                ".html"  // should be last
            };
            foreach (var ext in exts)
            {
                var url = Url.Replace(".html", ext);
                var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
                var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
                byte[] result = null;
                if (src.Contains("://"))
                {  // for Android
                    var www = new WWW(src);
                    yield return www;
                    result = www.bytes;
                }
                else
                {
                    result = System.IO.File.ReadAllBytes(src);
                }
                System.IO.File.WriteAllBytes(dst, result);
                if (ext == ".html")
                {
                    webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
                    break;
                }
            }
        }
#else
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
        }
        webViewObject.EvaluateJS(
            "parent.$(function() {" +
            "   window.Unity = {" +
            "       call:function(msg) {" +
            "           parent.unityWebView.sendMessage('WebViewObject', msg)" +
            "       }" +
            "   };" +
            "});
        ");
#endif
        yield break;
    }

    public async void Authentic(TokenResponse token)
    {
        user = client.ParseJwtToken(token.AccessToken, true);

        GameObject userInfoTextObject = GameObject.Find("Canvas/Panel/BackGroundImage/UserInfoText");
        if (userInfoTextObject != null)
        {

            userInfoText = userInfoTextObject.GetComponent<TextMeshProUGUI>();
            //Debug.Log(userInfoText.text);
            if (userInfoText != null)
            {

                userInfoText.text = $"Name: {user.Name}\n" +
                                    $"Owner: {user.Owner}\n" +
                                    $"Created Time: {user.CreatedTime}\n" +
                                    $"Id: {user.Id}\n";
                userInfo = $"{user.Name}";
                avatarUrl = user.Avatar;
                _ = StartCoroutine(LoadTextureFromUrl(url: avatarUrl));
                InputVisibility(false);
                ToggleUserAvatarVisibility(true);

            }
            else
            {
                Debug.LogError("UserInfoText is null.");
            }
            Debug.Log(userInfoText.text);
        }
        else
        {
            Debug.LogError("UserInfoTextObject is null.");
        }
        LoginVisibility(false);
        StartCoroutine(LoadMainSceneWithDelay());
    }

    public async void OnCasdoorLogin()
    {
        Debug.Log("Casdoor Login Button clicked.");

        webViewObject.SetVisibility(true);

        if (!string.IsNullOrEmpty(callbackUrl))
        {
            while (string.IsNullOrEmpty(code))
            {
                await Task.Delay(10);  // If code is empty, continue checking after a short delay
            }
            if (!string.IsNullOrEmpty(code))
            {
                webViewObject.SetVisibility(false);

                Uri uri = new Uri(callbackUrl);
                callbackBase = uri.GetLeftPart(UriPartial.Path); // http://localhost:5000/callback
                Debug.Log(callbackBase);
                Debug.Log(code);

                TokenResponse token = await client.RequestAuthorizationCodeTokenAsync(code, callbackBase);
                if (token is null || token.AccessToken is null)
                {
                    Debug.LogError("Failed to get the token.");
                    return;
                }

                Authentic(token);


            }
            else
            {
                Debug.LogError("code is null or empty.");
            }
        }
        else
        {
            Debug.LogError("callbackUrl is null or empty.");
        }
    }

    private void ToggleUserAvatarVisibility(bool visible)
    {
        if (userAvatarImage != null)
        {
            userAvatarImage.gameObject.SetActive(visible);
        }
    }

    private void LoginVisibility(bool visible)
    {
        if (loginPanel != null)
        {
            loginPanel.gameObject.SetActive(visible);
        }
    }

    private void InputVisibility(bool visible)
    {
        if (inputPanel != null)
        {
            inputPanel.gameObject.SetActive(visible);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static IEnumerator LoadTextureFromUrl(string url)
    {
        using WWW www = new WWW(url);
        yield return www;

        if (string.IsNullOrEmpty(www.error))
        {
            if (userAvatarImage != null)
            {
                userAvatarImage.texture = www.texture;
            }
            else
            {
                Debug.LogError("UserAvatarImage is null.");
            }
        }
        else
        {
            Debug.LogError($"Failed to load texture from URL: {www.error}");
        }
    }


    public async void OnLogin()
    {
        //Debug.Log("Get tokens by username and password...");

        string account = accountInput.text;
        string password = passwordInput.text;

        token = await client.RequestPasswordTokenAsync(account, password);
        if (token is null || token.AccessToken is null)
        {
            Debug.LogError("Failed to get the token.");
            return;
        }

        Authentic(token);
        
    }

    private IEnumerator LoadMainSceneWithDelay()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene("Main");

        yield return new WaitForSeconds(0.5f);
        userInfoText.text = "";
        InputVisibility(true);
        ToggleUserAvatarVisibility(false);
        LoginVisibility(true);
    }
}
