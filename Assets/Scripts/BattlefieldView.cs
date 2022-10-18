using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System;
using SFB;

public class BattlefieldView : MonoBehaviour
{
    SimuData simuData;
    GameState gameState;

    GameObject[,] hexCells;
    GameObject[] spawners;
    public GameObject prefabHexCell;
    public Vector3 hexCellsStartPosition;

    public GameObject textLog;
    public GameObject textLogScrollView;

    public Text textTargetCellStatus;
    public GameObject panelTargetCell;
    public UIPersonInfo persInfoTargetedPerson;

    public Text textSelectedPersonStatus;
    public GameObject panelSelectedPerson;
    public UIPersonInfo persInfoSelectedPerson;

    public GameObject panelActivePersonMenu;

    public GameObject PrefabPersonSpawner;
    public GameObject CharSpawnersView;

    public static BattlefieldView instance = null;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance == this)
        {
            Destroy(gameObject); // Удаляем объект
        }
        DontDestroyOnLoad(gameObject);

        simuData = this.gameObject.AddComponent<SimuData>() as SimuData;
        gameState = this.gameObject.AddComponent<GameState>() as GameState;
        gameState.gameMode = GameState.GameMode.None;

        int cell_width = simuData.GetCellWidth();
        int cell_height = simuData.GetCellHeight();
        hexCells = new GameObject[cell_height, cell_width];
        for (int i = 0; i < cell_height; i++)
            for (int j = 0; j < cell_width; j++)
            {
                if (simuData.IsCellEnabled(i, j))
                {
                    hexCells[i, j] = Instantiate(prefabHexCell, hexCellsStartPosition + new Vector3(j * 1.3325f - i * 0.31f - ((i % 2 == 1) ? 0.66f : 0), -(i * 0.576f + j * 0.183f - ((i % 2 == 1) ? 0.09f : 0f)), 0), Quaternion.identity);
                    hexCells[i, j].transform.SetParent(this.transform);
                    hexCells[i, j].GetComponent<HexCellController>().SetDimensions(new Vector2Int(i, j));
                    hexCells[i, j].GetComponent<HexCellController>().SetCellColorClear();
                    hexCells[i, j].GetComponent<HexCellController>().SetCellFrameClear();
                }
            }
        RefreshTargetPanel();
        RefreshActiveCharPanel();
        HideActivePersonMenu();
        
        //Fill person spawners
        int cntr = 0;
        spawners = new GameObject[simuData.GetCharactersCount()];
        GameObject holder = null;
        foreach (SimuData.CharData cd in simuData.GetCharactersAll())
        {
            if (cntr % 2 == 0)
            {
                holder = new GameObject("holder_"+ (cntr / 2).ToString());
                holder.transform.SetParent(CharSpawnersView.transform);
                holder.transform.localPosition = new Vector3(10, (cntr / 2)*10);
                holder.transform.localScale = Vector3.one;
            }
            spawners[cntr] = Instantiate(PrefabPersonSpawner, CharSpawnersView.transform.position + new Vector3(13f + (cntr % 2)*26f, -16f-(cntr / 2) * 31f, 0), Quaternion.identity);
            spawners[cntr].transform.SetParent(holder.transform);
            spawners[cntr].transform.localScale = Vector3.one;
            spawners[cntr].GetComponent<PersonSpawnerController>().LoadPersonView(cd);
            spawners[cntr].GetComponent<PersonSpawnerController>().SetBFView(this);
            cntr++;
        }
        float element_y_size = spawners[0].GetComponent<RectTransform>().rect.size.y;
        CharSpawnersView.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((cntr - 1) / 2 + 1) * (element_y_size + 0.5f));

        AddLog("Запуск приложения...");
        LoadFieldConfigDefault();
    }

    #region AsyncLoadSpriteFunctions
    private IEnumerator LoadSpriteFromFileAndUpdateHexCharacter(string filename, HexCellController hc)
    {
        UnityWebRequest uwr = null;
        try
        {
            uwr = UnityWebRequestTexture.GetTexture(Application.streamingAssetsPath + "/Characters/" + filename);
        }
        catch (Exception ex)
        {
            AddLog("Ошибка во время чтения файла с описаниями классов: " + ex.Message);
            yield break;
        }

        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError || uwr.isHttpError)
        {
            Debug.Log(uwr.error);
        }
        else
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
            Sprite fromTex = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            hc.SetCharacterSprite(fromTex);
        }
    }
    
    public static IEnumerator LoadSpriteFromFileAndUpdateUIImage(string filename, Image img)
    {
        UnityWebRequest uwr = null;
        try
        {
            uwr = UnityWebRequestTexture.GetTexture(Application.streamingAssetsPath + "/Characters/" + filename);
        }
        catch (Exception ex)
        {
            BattlefieldView.instance.AddLog("Ошибка во время чтения файла с описаниями классов: " + ex.Message);
            yield break;
        }

        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError || uwr.isHttpError)
        {
            Debug.Log(uwr.error);
        }
        else
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
            Sprite fromTex = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            img.sprite = fromTex;
        }
    }
    #endregion

    #region Cell Color Procedures
    void ShowMoveableCells(List<Vector2Int> moveable_cells_list)
    {
        foreach (Vector2Int cell_coords in moveable_cells_list)
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorMoveable();
    }

    void ShowAttackableCells(List<Vector2Int> attackable_cells_list)
    {
        foreach (Vector2Int cell_coords in attackable_cells_list)
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorAttackable();
    }

    void ShowSkillableCells(List<Vector2Int> skillable_cells_list)
    {
        foreach (Vector2Int cell_coords in skillable_cells_list)
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorSkillable();
    }

    void ClearMoveableCells()
    {
        foreach (Vector2Int cell_coords in simuData.GetMovementPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
        simuData.ClearMovementPoints();
    }

    void ClearAttackableCells()
    {
        foreach (Vector2Int cell_coords in simuData.GetAttackPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
        simuData.ClearAttackPoints();
    }

    void ClearSkillableCells()
    {
        foreach (Vector2Int cell_coords in simuData.GetSkillPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
        simuData.ClearSkillPoints();
    }

    void ClearAllPersonalCells()
    {
        ClearMoveableCells();
        ClearAttackableCells();
        ClearSkillableCells();
    }

    void ClearAllCellsButNotData()
    {
        foreach (Vector2Int cell_coords in simuData.GetMovementPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
        foreach (Vector2Int cell_coords in simuData.GetAttackPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
        foreach (Vector2Int cell_coords in simuData.GetSkillPoints())
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellColorClear();
    }
    #endregion

    #region ControlsVisibility
    void RefreshTargetPanel()
    {
        if (simuData.IsActiveCellExist())
        {
            panelTargetCell.SetActive(true);
            textTargetCellStatus.gameObject.SetActive(true);
        }
        else
        {
            panelTargetCell.SetActive(false);
            textTargetCellStatus.gameObject.SetActive(false);
        }
    }

    void RefreshActiveCharPanel()
    {
        if (simuData.IsActivePersonExist())
        {
            panelSelectedPerson.SetActive(true);
            persInfoSelectedPerson.SetPersonInfo(simuData.GetPersonActive());
            textSelectedPersonStatus.text = "Выбранная особа: клетка " + simuData.GetActivePersonCoords();
        }
        else
        {
            persInfoSelectedPerson.ClearPersonInfo();
            textSelectedPersonStatus.text = "Выбранная особа: не выбрано";
            panelSelectedPerson.SetActive(false);
        }
    }

    void ShowActivePersonMenu()
    {
        panelActivePersonMenu.SetActive(true);
    }

    void HideActivePersonMenu()
    {
        panelActivePersonMenu.SetActive(false);
    }
    #endregion

    #region Buttons
    public void GoToSelectedCoords()
    {
        Vector2Int activePersonCoords = simuData.GetActivePersonCoords();
        if (simuData.IsActivePersonCanGoToActiveCell()) //проверка на радиус движения
        {
            hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>().RemoveCharacterSprite();
            hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>().SetCellFrameClear();
            ClearAllPersonalCells();
            simuData.MoveActiveCharacterToActiveCell();
            simuData.FillAllPointsForActivePerson();
            ShowActivePersonMenu();

            SimuData.CharData adata = simuData.GetActivePersonCharData();
            string sprite_file = simuData.IsActivePersonInUpperHalfOfField() ? adata.sprite_front : adata.sprite_back;
            activePersonCoords = simuData.GetActivePersonCoords();
            hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>().SetCellFrameActivePerson();
            StartCoroutine(LoadSpriteFromFileAndUpdateHexCharacter(sprite_file, hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>()));
            RefreshActiveCharPanel();
            RefreshTargetPanel();
            gameState.gameMode = GameState.GameMode.None;
            IncreaseActivePersonTurnCounterAndCheckForPassiveSkill();
            SaveFieldConfigDefault();
            AddLog("Передвинута особа " + adata.name + " в клетку " + activePersonCoords);
        }
        else
        {
            AddLog("Нельзя перейти на расстояние, большее вашего радиуса передвижения!");
        }
    }

    void PlacePersonToCellAndMakeActive(Vector2Int cell_coords, string character_name)
    {
        SpawnPersonAtCell(cell_coords, character_name);

        if (simuData.IsActivePersonExist()) //очистить прошлую выбраную особу, если есть
        {
            Vector2Int activePersonCoords = simuData.GetActivePersonCoords();
            hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>().SetCellFrameClear();
            ClearAllPersonalCells();
        }

        simuData.PlacePersonToCellAndMakeActive(cell_coords, character_name);
        ClearTargetFrameAndDataIfExist();

        simuData.FillAllPointsForActivePerson();
        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameActivePerson();

        ShowActivePersonMenu();
        RefreshActiveCharPanel();
        RefreshTargetPanel();
        AddLog("Выставлена особа " + character_name);
        gameState.gameMode = GameState.GameMode.None;
        SaveFieldConfigDefault();
    }

    void SpawnPersonAtCell(Vector2Int cell_coords, string character_name)
    {
        StartCoroutine(LoadSpriteFromFileAndUpdateHexCharacter(simuData.GetCharFieldSpriteByName(character_name, cell_coords.y), hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>()));
    }

    public void PlacePersonIfCellIsFree(Vector2Int cell_coords, string character_name)
    {
        if (!simuData.IsAnyPersonInThisCell(cell_coords))
        {
            PlacePersonToCellAndMakeActive(cell_coords, character_name);
        }
        else
            AddLog("Нельзя выставить особу - клетка уже занята!");
    }

    void ClearSelectedCell(Vector2Int cell_coords)
    {
        //развыбор меты
        simuData.DeselectActiveCell();
        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameClear();
        RefreshActiveCharPanel();
    }

    public void ClickOnCell(Vector2Int cell_coords)
    {
        if (simuData.IsActivePersonInThisCell(cell_coords))
        {
            //развыбор и убирание клеток атаки/передвижения/навыка
            hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameClear();
            ClearAllPersonalCells();
            simuData.DeselectActivePerson();
            ClearTargetFrameAndDataIfExist();

            HideActivePersonMenu();

            RefreshActiveCharPanel();
            gameState.gameMode = GameState.GameMode.None;
            AddLog("Выбран режим выбора/выставления");
        }
        else if (simuData.IsCellActive(cell_coords))
        {   
                switch (gameState.gameMode)
                {
                    case GameState.GameMode.None:
                        ClearSelectedCell(cell_coords);
                        break;
                    case GameState.GameMode.Attack:
                        if (simuData.IsAnyPersonInActiveCell())
                            AttackPerson();
                        else
                            ClearSelectedCell(cell_coords);
                        break;
                case GameState.GameMode.Skill:
                    if (simuData.IsAnyPersonInActiveCell())
                        UseSkillOnPerson();
                    else
                        ClearSelectedCell(cell_coords);
                    break;
                case GameState.GameMode.Movement:
                    if (!simuData.IsAnyPersonInActiveCell())
                        GoToSelectedCoords();
                    else
                        AddLog("Нельзя перейти в место, где уже стоит другая особа!");
                    break;
            }
        }
        else //ести клетка не метная и не с выбранной особой
        {
            ClearTargetFrameAndDataIfExist();
            if (simuData.IsAnyPersonInThisCell(cell_coords))
            {
                switch (gameState.gameMode)
                {
                    case GameState.GameMode.None:
                        if (simuData.IsActivePersonExist()) //если особа не выбрана - чистим оправу у прошлой выбранной особы
                        {
                            Vector2Int activePersonCoords = simuData.GetActivePersonCoords();
                            hexCells[activePersonCoords.x, activePersonCoords.y].GetComponent<HexCellController>().SetCellFrameClear();
                            ClearAllPersonalCells();
                        }
                        //выбрать новую особу
                        simuData.SelectActivePerson(cell_coords);
                        simuData.FillAllPointsForActivePerson();
                        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameActivePerson();
                        RefreshActiveCharPanel();
                        ShowActivePersonMenu();
                        break;
                    case GameState.GameMode.Attack:
                        simuData.SelectActiveCell(cell_coords);
                        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameActiveCell();
                        textTargetCellStatus.text = "Наведённая клетка: " + simuData.GetActiveCell() + " (занята)";
                        persInfoTargetedPerson.gameObject.SetActive(true);
                        RefreshTargetPanel();
                        AttackPerson();
                        break;
                    case GameState.GameMode.Skill:
                        simuData.SelectActiveCell(cell_coords);
                        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameActiveCell();
                        textTargetCellStatus.text = "Наведённая клетка: " + simuData.GetActiveCell() + " (занята)";
                        persInfoTargetedPerson.gameObject.SetActive(true);
                        RefreshTargetPanel();
                        UseSkillOnPerson();
                        break;
                    case GameState.GameMode.Movement:
                        AddLog("Нельзя перейти в место, где уже стоит другая особа!");
                        break;
                }
            }
            else //если клетка пустая - выбрать
            {
                switch (gameState.gameMode)
                {
                    case GameState.GameMode.None:
                    case GameState.GameMode.Attack:
                    case GameState.GameMode.Skill:
                        simuData.SelectActiveCell(cell_coords);
                        hexCells[cell_coords.x, cell_coords.y].GetComponent<HexCellController>().SetCellFrameActiveCell();
                        textTargetCellStatus.text = "Наведённая клетка: " + simuData.GetActiveCell() + " (свободна)";
                        persInfoTargetedPerson.gameObject.SetActive(false);
                        AddLog("Клетка " + cell_coords + " выбрана");
                        break;
                    case GameState.GameMode.Movement:
                        simuData.SelectActiveCell(cell_coords);
                        GoToSelectedCoords();
                        break;
                }
            }
            //HideAddPersonControls();
        }
        RefreshTargetPanel();
    }

    public void RemovePerson()
    {
        if (simuData.IsActivePersonExist())
        {
            Vector2Int coords = simuData.GetActivePersonCoords();
            ClearAllPersonalCells();
            ClearTargetFrameAndDataIfExist();
                
            hexCells[coords.x, coords.y].GetComponent<HexCellController>().RemoveCharacterSprite();
            hexCells[coords.x, coords.y].GetComponent<HexCellController>().SetCellFrameClear();
            simuData.RemovePersonFromCell(coords);
            simuData.DeselectActivePerson();

            RefreshTargetPanel();
            RefreshActiveCharPanel();
            HideActivePersonMenu();
            gameState.gameMode = GameState.GameMode.None;
            AddLog("Убрана особа из клетки " + coords);
            SaveFieldConfigDefault();
        }
        else
            AddLog("Нельзя убрать особу, так как нету выбранной клетки!");
    }

    private void ClearTargetFrameAndDataIfExist()
    {
        if (simuData.IsActiveCellExist()) // убирание прицела, если есть
        {
            Vector2Int ac = simuData.GetActiveCell();
            hexCells[ac.x, ac.y].GetComponent<HexCellController>().SetCellFrameClear();
            simuData.DeselectActiveCell();
        }
    }

    public void RemoveAllPersons()
    {
        foreach (GameObject hex_cell in hexCells)
            if (hex_cell != null)
            {
                Vector2Int coords = hex_cell.GetComponent<HexCellController>().GetDimensions();
                if (simuData.IsAnyPersonInThisCell(coords))
                {
                    hex_cell.GetComponent<HexCellController>().RemoveCharacterSprite();
                    if (simuData.IsActivePersonInThisCell(coords))
                    {
                        ClearAllPersonalCells();
                        simuData.DeselectActivePerson();
                        hexCells[coords.x, coords.y].GetComponent<HexCellController>().SetCellFrameClear();
                    }
                    simuData.RemovePersonFromCell(coords);
                }
                if (simuData.IsCellActive(coords))
                {
                    hexCells[coords.x, coords.y].GetComponent<HexCellController>().SetCellFrameClear();
                    simuData.DeselectActiveCell();
                }
            }
        ClearTargetFrameAndDataIfExist();
        RefreshTargetPanel();
        RefreshActiveCharPanel();
        HideActivePersonMenu();
        gameState.gameMode = GameState.GameMode.None;
        AddLog("Игровое поле полностью очищено");
        SaveFieldConfigDefault();
    }

    public void GameModeAttack()
    {
        gameState.gameMode = GameState.GameMode.Attack;
        ClearAllCellsButNotData();
        ShowAttackableCells(simuData.GetAttackPoints());
        AddLog("Выбран режим атаки");
    }

    public void GameModeMovement()
    {
        gameState.gameMode = GameState.GameMode.Movement;
        ClearAllCellsButNotData();
        ShowMoveableCells(simuData.GetMovementPoints());
        AddLog("Выбран режим передвижения");
    }

    public void GameModeSkill()
    {
        gameState.gameMode = GameState.GameMode.Skill;
        ClearAllCellsButNotData();
        ShowSkillableCells(simuData.GetSkillPoints());
        AddLog("Выбран режим навыка");
    }

    void AttackPerson()
    {
        if (simuData.IsActivePersonCanAttackActiveCell())
        {
            bool is_crit, is_dmg_incr;
            int damage = simuData.AttackPersonInActiveCellByActivePerson(out is_crit, out is_dmg_incr);

            //для подробного лога
            Vector2Int target_coords = simuData.GetActiveCell();
            Vector2Int attacker_coords = simuData.GetActivePersonCoords();
            SimuData.PersonData personDataTarget = simuData.GetPersonByCoords(target_coords);
            SimuData.PersonData personDataAttacker = simuData.GetPersonActive();
            AddLog("Особой \"" + personDataAttacker.charData.name + "\" из клетки " + attacker_coords.ToString() + " нанесён урон в " + damage.ToString() + " единиц по особе \"" +
                    personDataTarget.charData.name + "\" из клетки " + target_coords.ToString() +
                    (is_dmg_incr ? " (урон увеличен благодаря пассивному навыку " + personDataAttacker.charData.passive_skill + ")" : ""));

            hexCells[attacker_coords.x, attacker_coords.y].GetComponent<HexCellController>().SpawnAttackIcon();
            hexCells[target_coords.x, target_coords.y].GetComponent<HexCellController>().SpawnDamageIcon(damage, is_crit);

            //проверка на смерть цели
            if (simuData.IsPersonInActiveCellAlive())
            {
                persInfoTargetedPerson.SetPersonInfo(simuData.GetPersonByCoords(target_coords));
            }
            else
            {
                hexCells[target_coords.x, target_coords.y].GetComponent<HexCellController>().RemoveCharacterSprite();
                hexCells[target_coords.x, target_coords.y].GetComponent<HexCellController>().SetCellFrameClear();
                simuData.RemovePersonFromCell(target_coords);
                simuData.DeselectActiveCell();
                RefreshTargetPanel();
                AddLog("Особа \"" + personDataTarget.charData.name + "\" в клетке " + target_coords.ToString() + " погибает!");
            }

            IncreaseActivePersonTurnCounterAndCheckForPassiveSkill();
            SaveFieldConfigDefault();
        }
        else
        {
            persInfoTargetedPerson.SetPersonInfo(simuData.GetPersonByCoords(simuData.GetActiveCell()));
            AddLog("Нельзя ударить цель вне вашего радиуса атаки!");
        }
    }

    public void UseSkillOnPerson()
    {
        if (simuData.IsActivePersonCanUseSkillOnActiveCell())
        {
            AddLog("Навыки покамест не прописаны!");
            IncreaseActivePersonTurnCounterAndCheckForPassiveSkill();
            SaveFieldConfigDefault();
        }
        else
            AddLog("Нельзя применить навык вне вашего радиуса навыка!");
    }

    void IncreaseActivePersonTurnCounterAndCheckForPassiveSkill()
    {
        simuData.IncreaseInternalTurnCounterOfActivePerson();
        SimuData.PersonData pd = simuData.GetPersonActive();
        switch (pd.charData.passive_skill.ToLower())
        {
            case "крепкая настойка":
                if (pd.internalTurnCounter % 3 == 0)
                {
                    int heal_hp = simuData.HealActivePersonByHP(100);
                    RefreshActiveCharPanel();
                    AddLog("Срабатывает пассивный навык крепкая настойка: здоровье активной особы восстанавливается на " + heal_hp.ToString());
                }
                break;
        }
    }
    #endregion

    #region SaveLoadFieldConfig
    public void LoadFieldFromFile()
    {
        try
        {
            string dir_path = System.IO.Directory.GetCurrentDirectory();
            var extensions = new[] {
                new ExtensionFilter("Текстовые файлы", "txt"),
                new ExtensionFilter("Все файлы", "*" ),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Введите название файла сохранения", dir_path, extensions, false, "field_config_default.txt");
            if (paths.Length > 0)
            {
                LoadFieldConfigFromFile(paths[0]);
            }
            else
                AddLog("Не выбрано название файла для чтения конфигурации!");
        }
        catch (Exception ex)
        {
            AddLog("Ошибка при открытии файла конфигурации " + ex.Message);
        }
    }

    public void SaveFieldToFile()
    {
        try
        {
            string dir_path = System.IO.Directory.GetCurrentDirectory();
            var extensions = new[] {
                new ExtensionFilter("Текстовые файлы", "txt"),
                new ExtensionFilter("Все файлы", "*" ),
            };
            var path = StandaloneFileBrowser.SaveFilePanel("Save File", dir_path, "field_config_" + DateTime.Now.ToString("yyyy.MM.dd_\\hhh\\mmm\\sss") + ".txt", extensions);
            if (path!="")
            {
                SaveFieldConfigToFile(path);
                AddLog("Взаиморасположение и состояние поля записано в файл " + path);
            }
            else
                AddLog("Не выбрано название файла для записи конфигурации!");
        }
        catch (Exception ex)
        {
            AddLog("Ошибка при записи файла конфигурации " + ex.Message);
        }
    }

    void SaveFieldConfigToFile(string filename)
    {
        string data = "";
        SimuData.PersonData pd;
        for (int i = 0; i < simuData.GetCellHeight(); i++)
            for (int j = 0; j < simuData.GetCellWidth(); j++)
            {
                if (simuData.IsAnyPersonInThisCell(new Vector2Int(i, j)))
                {
                    pd = simuData.GetPersonByCoords(new Vector2Int(i, j));
                    data += i.ToString() + "," + j.ToString() + "," + pd.charData.name + "," + pd.charData.race + "," + pd.charData.class_of_char + "," +
                        pd.charData.sprite + "," + pd.charData.sprite_front + "," + pd.charData.sprite_back + "," +
                        pd.currentHealth.ToString() + "," + pd.charData.health.ToString() + "," + pd.charData.speed.ToString() + "," + pd.charData.initiative.ToString() + "," +
                        pd.charData.attack.ToString() + "," + pd.charData.attack_radius.ToString() + "," + pd.charData.attack_radius_min.ToString() + "," +
                        pd.charData.critical_percent.ToString() + "," + pd.charData.defense_percent.ToString() + "," + pd.charData.skill_radius.ToString() + "," + 
                        pd.charData.active_skill + "," + pd.charData.passive_skill + "\n";
                }
            }

        //string path = System.IO.Directory.GetCurrentDirectory() + "/";
        try
        {
            //if (!Directory.Exists(path))
                //Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(filename, data);
        }
        catch (System.Exception ex)
        {
            AddLog("Ошибка записи взаиморасположения и состояния поля " + ex.Message);
        }
    }

    void LoadFieldConfigFromFile(string filename)
    {
        //string path = System.IO.Directory.GetCurrentDirectory() + "/";
        string data = "";
        if (File.Exists(filename))
        {
            data = System.IO.File.ReadAllText(filename);
            AddLog("Взаиморасположение и состояние поля считано из файла " + filename);
        }
        else
        {
            AddLog("Отсутствует файл взаиморасположения и состояния поля " + filename + "!");
            return;
        }

        RemoveAllPersons();
        using (var reader = new StringReader(data))
        {
            SimuData.CharData cd;
            SimuData.PersonData pd;
            string[] values;
            for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                try
                {
                    values = line.Split(',');
                    cd = new SimuData.CharData();
                    cd.name = values[2];
                    cd.race = values[3];
                    cd.class_of_char = values[4];
                    cd.sprite = values[5];
                    cd.sprite_front = values[6];
                    cd.sprite_back = values[7];
                    cd.health = Convert.ToInt32(values[9]);
                    cd.speed = Convert.ToInt32(values[10]);
                    cd.initiative = Convert.ToInt32(values[11]);
                    cd.attack = Convert.ToInt32(values[12]);
                    cd.attack_radius = Convert.ToInt32(values[13]);
                    cd.attack_radius_min = Convert.ToInt32(values[14]);
                    cd.critical_percent = Convert.ToInt32(values[15]);
                    cd.defense_percent = Convert.ToInt32(values[16]);
                    pd = new SimuData.PersonData(cd);
                    pd.currentHealth = Convert.ToInt32(values[8]);
                    cd.skill_radius = Convert.ToInt32(values[17]);
                    cd.active_skill = values[18];
                    cd.passive_skill = values[19];
                    simuData.PlacePersonToCellByData(new Vector2Int(Convert.ToInt32(values[0]), Convert.ToInt32(values[1])), pd);
                }
                catch (System.Exception ex)
                {
                    AddLog("Ошибка чтения строки файла взаиморасположения " + ex.Message);
                }
            }
        }
        for (int i = 0; i < simuData.GetCellHeight(); i++)
            for (int j = 0; j < simuData.GetCellWidth(); j++)
            {
                Vector2Int vec = new Vector2Int(i, j);
                if (simuData.IsAnyPersonInThisCell(vec))
                {
                    SpawnPersonAtCell(vec, simuData.GetPersonByCoords(vec).charData.name);
                }
            }
        RefreshTargetPanel();
        RefreshActiveCharPanel();
        HideActivePersonMenu();
        SaveFieldConfigDefault();
        AddLog("Взаиморасположение и состояние особ загружено из файла " + filename);
    }

    void LoadFieldConfigDefault()
    {
        LoadFieldConfigFromFile("field_config_default.txt");
    }

    void SaveFieldConfigDefault()
    {
        SaveFieldConfigToFile("field_config_default.txt");
    }
    #endregion

    public void AddLog(string txt)
    {
        Debug.Log(txt);
        string msgText = textLog.GetComponent<Text>().text;
        if (msgText != "")
            msgText += "\n";
        msgText += txt;
        textLog.GetComponent<Text>().text = msgText;
        Canvas.ForceUpdateCanvases();
        textLogScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemovePerson();
        }
    }
}
