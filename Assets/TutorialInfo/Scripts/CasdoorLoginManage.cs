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


    // Start is called before the first frame update
    private void Start()
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
        Debug.Log("Get tokens by username and password...");

        string account = accountInput.text;
        string password = passwordInput.text;

        token = await client.RequestPasswordTokenAsync(account, password);
        if (token is null || token.AccessToken is null)
        {
            Debug.LogError("Failed to get the token.");
            return;
        }
        else
        {
            Debug.Log("Get tokens by username and password success.");
            Debug.Log($"token.AccessToken : {token.AccessToken}\n" +
                $"token.RefreshToken : {token.RefreshToken}\n" +
                $"token.IdentityToken : {token.IdentityToken}\n" +
                $"token.Scope : {token.Scope}\n" +
                $"token.ExpiresIn : {token.ExpiresIn}\n" + 
                $"token.TokenType : {token.TokenType}\n");
        }

        user = client.ParseJwtToken(token.AccessToken, true);
        Debug.Log($"user.Name : {user.Name}\n" +
            $"user.Owner : {user.Owner}\n" +
            $"user.CreatedTime : {user.CreatedTime}\n" +
            $"user.Id : {user.Id}\n"
            );

        GameObject userInfoTextObject = GameObject.Find("Canvas/Panel/BackGroundImage/UserInfoText");
        if (userInfoTextObject != null)
        {
            
            userInfoText = userInfoTextObject.GetComponent<TextMeshProUGUI>();
            Debug.Log(userInfoText.text);
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
