// PitchSettingsManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchSettingsManager : MonoBehaviour
{
    public List<BallSettings> fastballSettings;
    public List<BallSettings> sliderSettings;
    public List<BallSettings> curveSettings;
    public List<BallSettings> changeupSettings;

    void Awake()
    {
        // 초기 설정 리스트 초기화
        fastballSettings = new List<BallSettings>();
        sliderSettings = new List<BallSettings>();
        curveSettings = new List<BallSettings>();
        changeupSettings = new List<BallSettings>();

        // 각 구종에 대한 설정 추가
        AddPitchSettings();
    }
    void AddPitchSettings()
    {
        // Fastball settings                  throw power, 
        fastballSettings.Add(new BallSettings(70, 70, -1300f, 0, 11.8f, 0, 0, 0, 0, 0));
        fastballSettings.Add(new BallSettings(80, 80, -1500f, 0, 8.2f, 0, 0, 0,0, 0));
        fastballSettings.Add(new BallSettings(90, 90, -1700f, 0, 5.9f, 0, 0, 0,0, 0));
        fastballSettings.Add(new BallSettings(100, 100, -1900f, 0, 4.2f, 0, 0, 0,0, 0));
        fastballSettings.Add(new BallSettings(110, 110, -2000f, 0, 3, 0, 0, 0, 0,0));
        fastballSettings.Add(new BallSettings(120, 120, -2100f, 0, 2.4f, 0, 0, 0,0 , 0));
        fastballSettings.Add(new BallSettings(130, 130, -2200f, 0, 2, 0,0, 0, 0, 0));
        fastballSettings.Add(new BallSettings(140, 140, -2300f, 0, 1, 0, 0, 0,0, 0));
        fastballSettings.Add(new BallSettings(150, 150, -2400f, 0, 1, 0,0, 0, 0, 0));
        fastballSettings.Add(new BallSettings(160, 160, -2500, 0, 0.5f, 0,0, 0, 0, 0));
        fastballSettings.Add(new BallSettings(170, 170, -2600, 0, 0, 0, 0, 0,0, 0));

        // Slider settings
        sliderSettings.Add(new BallSettings(70, 70, -1300f, -1300, 11.8f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(80, 80, -1400f, -1400, 8.2f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(90, 90, -1600f, -1600f, 5.9f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(100, 100, -1900f, -1900f, 4.2f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(110, 110, -2000f, -2000f, 3, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(120, 120, -2100, -2100f, 2.4f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(130, 130, -2200f, -2200f,  2, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(140, 140, -2300, -2300f,  1, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(150, 150, -2400, -2400f,  1, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(160, 160, -2500, -2500,  0.5f, 0, 45, 45, 0, 0));
        sliderSettings.Add(new BallSettings(170, 170, -2600, -2600, 0, 0, 45, 45, 0, 0));

        // Curve settings
        curveSettings.Add(new BallSettings(70, 70, 1300f, 0, 15.3f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(80, 80, 1500f, 0, 11.7f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(90, 90, 1700f, 0, 9.4f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(100, 100, 1900f, 0, 7.7f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(110, 110, 2000f, 0, 6.5f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(120, 120, 2100f, 0, 5.9f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(130, 130, 2200f, 0, 5.5f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(140, 140, 2300f, 0, 4.5f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(150, 150, 2400f, 0, 4.5f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(160, 160, 2500, 0, 4.0f, 0, 0, 0, 0, 0));
        curveSettings.Add(new BallSettings(170, 170, 2600, 0, 3.5f, 0, 0, 0, 0, 0));

        // Changeup settings
        changeupSettings.Add(new BallSettings(70, 70, -1100f, 1100, 11.8f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(80, 80, -1200f, 1200, 8.2f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(90, 90, -1400f, 1400f, 5.9f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(100, 100, -1700f, 1700f, 4.2f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(110, 110, -1800f, 1800f, 3, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(120, 120, -1900, 1900f, 2.4f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(130, 130, -2000f, 2000f, 2, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(140, 140, -2100, 2100f, 1, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(150, 150, -2200, 2200f, 1, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(160, 160, -2300, 2300, 0.5f, 0, 45, -45, 0, 0));
        changeupSettings.Add(new BallSettings(170, 170, -2400, 2400, 0, 0, 45, -45, 0, 0));
    }
}