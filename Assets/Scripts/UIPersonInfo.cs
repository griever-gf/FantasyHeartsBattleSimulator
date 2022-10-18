using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPersonInfo : MonoBehaviour
{
    public Image personPortrait;
    public Text personInfo;

    public void SetPersonInfo(SimuData.PersonData person_data)
    {
        SimuData.CharData char_data = person_data.charData;
        StartCoroutine(BattlefieldView.LoadSpriteFromFileAndUpdateUIImage(char_data.sprite, personPortrait));
        personPortrait.preserveAspect = true;
        ResfreshPersonTextInfo(person_data);
    }

    public void ResfreshPersonTextInfo(SimuData.PersonData person_data)
    {
        SimuData.CharData char_data = person_data.charData;
        personInfo.text = "Имя: " + char_data.name + "\nКласс: " + char_data.class_of_char + "\nРаса: " + char_data.race +
                     "\nСкорость: " + char_data.speed.ToString() + "\nРетивость: " + char_data.initiative.ToString() +
                     "\nЗдоровье: " + person_data.currentHealth + "/" + char_data.health.ToString() + "\nЗащита: " + char_data.defense_percent.ToString() + "%" +
                     "\nАтака: " + char_data.attack.ToString() + "\nРадиус атаки: " + char_data.attack_radius.ToString() +
                     "\nКрит. вероят: " + char_data.critical_percent.ToString() + "%";
    }

    public void ClearPersonInfo()
    {
        personPortrait.sprite = null;
        personInfo.text = "";
    }
}
