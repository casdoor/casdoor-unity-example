﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public GameObject pillarPrefab;
    public GameObject pillarHolder;
    public GameObject cube;
    public GameObject loseUI;
    public GameObject nextLevelUI;
    public GameObject wonGameUI;
    public Text scoreText, finalScoreText, bestScoreText, jumpsText;
    public Text nextLevelScoreText;
    public Text levelText;
    public int pillarsToMake = 10;
    public float minDistance = 4f, maxDistance = 6f;
    public float minHeight = -9f, maxHeight = -7f;
    public int score, jumps;
    Vector3 spawnPosition;
    bool gameOver;
    int currentLevel = 1;

    public TextMeshProUGUI userInfoText;
    public RawImage userAvatarImage;

    // Use this for initialization
    void Start()
    {
        GameObject userInfoTextObject = GameObject.Find("Canvas/UserInfoText");
        if (userInfoTextObject != null)
        {

            userInfoText = userInfoTextObject.GetComponent<TextMeshProUGUI>();
            userInfoText.text = CasdoorLoginManage.userInfo;

        }
        else
        {
            Debug.LogError("UserInfoTextObject is null.");
        }
        if (userAvatarImage == null)
        {
            userAvatarImage = GameObject.Find("Canvas/UserAvatarImage").GetComponent<RawImage>();
        }
        _ = StartCoroutine(LoadTextureFromUrl(url: CasdoorLoginManage.avatarUrl));

        currentLevel = PlayerPrefs.GetInt("currentLevel", 1);
        levelText.GetComponent<Text>().text = "Level: " + currentLevel;
        spawnPosition = transform.position;
        minDistance += currentLevel;
        maxDistance += currentLevel;
        minHeight += currentLevel;
        maxHeight += currentLevel;
        pillarsToMake += (currentLevel * 3);
        makingPillars();
        score = 0;
        gameOver = false;
    }

    public void OnExit()
    {
        userInfoText.text = "";
        userAvatarImage = null;
        SceneManager.LoadScene("Login");

    }

    public IEnumerator LoadTextureFromUrl(string url)
    {
        using WWW www = new WWW(url);
        yield return www; // Wait for the image to load

        if (string.IsNullOrEmpty(www.error))
        {
            if (userAvatarImage != null)
            {
                userAvatarImage.texture = www.texture; // Set the picture as the texture of Raw Image
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

    void FixedUpdate()
    {
        if (cube.transform.position.y < -13 && !gameOver)
        {
            gameOver = true;
            Camera.main.gameObject.GetComponent<CameraBehaviour>().gameOver = true;
            StartCoroutine(loseGame());
        }
    }

    public void scoreUp(int points)
    {
        score += points;
        jumps++;
        scoreText.text = "" + points;
    }

    void makingPillars()
    {
        for (int i = 0; i < pillarsToMake; i++)
        {
            GameObject tempPillar = Instantiate(pillarPrefab, spawnPosition, pillarPrefab.transform.rotation);
            tempPillar.transform.SetParent(pillarHolder.transform);
            tempPillar.GetComponent<PillarBehaviour>().gameManager = this;
            tempPillar.GetComponent<PillarBehaviour>().cube = cube;
            tempPillar.GetComponent<PillarBehaviour>().score = i + 1;
            spawnPosition = new Vector3(spawnPosition.x, Random.Range(minHeight, maxHeight),
                            spawnPosition.z + Random.Range(minDistance, maxDistance));
            if (i == pillarsToMake - 1)
            {
                tempPillar.GetComponent<PillarBehaviour>().isLast = true;
            }
        }
    }

    IEnumerator loseGame()
    {
        cube.GetComponent<CubeBehaviour>().loseGame = true;
        for (int i = 0; i >= 0; i--)
        {
            float k = (float)i / 10;
            yield return new WaitForSeconds(.05f);
        }
        if (score > PlayerPrefs.GetInt("bestScore", 0)) PlayerPrefs.SetInt("bestScore", score);
        loseUI.SetActive(true);
        finalScoreText.GetComponent<Text>().text = "" + score;
        bestScoreText.GetComponent<Text>().text = "Best score:" + PlayerPrefs.GetInt("bestScore", 0);
        jumpsText.GetComponent<Text>().text = "Jumps:" + jumps;
    }

    public IEnumerator nextLevelPanel()
    {
        cube.GetComponent<CubeBehaviour>().loseGame = true;
        for (int i = 0; i >= 0; i--)
        {
            float k = (float)i / 10;
            yield return new WaitForSeconds(.05f);
        }
        if (currentLevel < 3)
        {
            PlayerPrefs.SetInt("currentLevel", currentLevel + 1);
            nextLevelUI.SetActive(true);
            nextLevelScoreText.GetComponent<Text>().text = "" + score;
        }
        else
        {
            PlayerPrefs.SetInt("currentLevel", 1);
            wonGameUI.SetActive(true);
        }
        
    }

    public void restart()
    {
        SceneManager.LoadScene("Main");
    }

    public void quit()
    {
        Application.Quit();
    }
}
