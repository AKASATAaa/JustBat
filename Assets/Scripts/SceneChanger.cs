using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Xml.Serialization;

public class SceneChanger : MonoBehaviour
{
    public FadeInOut fadeInOut;
        public void ToLiveBatting()
        // acredev1SceneChange 이름의 함수 선언
        {
        fadeInOut.FadeOut();
        Invoke("live", 0.5f);
            //SceneManager 메서드의 LoadScene 함수를 통해 acredev1.scene으로 씬 전환
        }
        public void ToTeeBatting()
    {
        fadeInOut.FadeOut();
        // acredev2SceneChanger
        Invoke("tee", 0.5f);
        //SceneManager 메서드의 LoadScene 함수를 통해 acredev2.scene으로 씬 전환
    }
    public void ToHomeRun()
    {
        fadeInOut.FadeOut();
        // acredev2SceneChanger
        Invoke("HR", 0.5f);
        //SceneManager 메서드의 LoadScene 함수를 통해 acredev2.scene으로 씬 전환
    }

    public void ResetScenePX4()
    {
        SceneManager.LoadScene("PX4TeeBatting");
    }

    public void live()
    {
        SceneManager.LoadScene("LiveBatting");
    }
    public void tee()
    {
        SceneManager.LoadScene("TeeBatting");
    }
    public void HR()
    {
        SceneManager.LoadScene("HomeRun");
    }
}
