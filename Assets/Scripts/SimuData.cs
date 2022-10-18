using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

public class SimuData : MonoBehaviour
{
    struct HexCell
    {
        public bool isActive;
    }

    const int cellNumberWidth = 10;
    const int cellNumberHeight = 10;

    int[,] enabledCells = new int[cellNumberHeight, cellNumberWidth] {
        { 0, 0, 1, 1, 1, 1, 1, 1, 0, 0 },
        { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0 },
        { 0, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
        { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
        { 0, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
        { 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
        { 0, 0, 1, 1, 1, 1, 1, 1, 0, 0 }
    };

    [Serializable]
    public class CharData
    {
        public string name { get; set; }
        public string race { get; set; }
        public string class_of_char { get; set; }
        public string sprite { get; set; }
        public string sprite_front { get; set; }
        public string sprite_back { get; set; }
        public int speed { get; set; }
        public int attack { get; set; }
        public int attack_radius { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(1)]
        public int attack_radius_min { get; set; }

        public int health { get; set; }
        public int critical_percent { get; set; }
        public int initiative { get; set; }
        public int defense_percent { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(1)]
        public int skill_radius { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string active_skill { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string passive_skill { get; set; }
    }

    public CharData[] characters;

    public class PersonData
    {
        public CharData charData;
        public int currentHealth;
        public int uniqueSpawnId;
        public int lastStrikedPersonId;
        public int internalTurnCounter;

        public PersonData(CharData cd)
        {
            charData = cd;

            currentHealth = cd.health;
            System.Random rnd = new System.Random();
            uniqueSpawnId = rnd.Next(1, Int32.MaxValue);
            lastStrikedPersonId = 0;
            internalTurnCounter = 0;
        }
    }

    PersonData[,] persons = new PersonData[cellNumberHeight, cellNumberWidth];

    Vector2Int ActiveCellCoords;
    Vector2Int ActivePersonCoords;

    List<Vector2Int> movementPotentialCells;
    List<Vector2Int> attackPotentialCells;
    List<Vector2Int> skillPotentialCells;

    public void Awake()
    {
        try
        {
            string jsonData = File.ReadAllText(Application.dataPath + "/StreamingAssets/characters_data.json");
            characters = JArray.Parse(jsonData).ToObject<CharData[]>();
        }
        catch (Exception ex)
        {
            gameObject.GetComponent<BattlefieldView>().AddLog("Ошибка во время чтения файла с описаниями персонажей: " + ex.Message);
        }
        //foreach (CharData ch in characters)
            //Debug.Log(ch.name + " (min radius): " + ch.attack_radius_min.ToString());
        DeselectActiveCell();
        DeselectActivePerson();
        movementPotentialCells = new List<Vector2Int>();
        attackPotentialCells = new List<Vector2Int>();
        skillPotentialCells = new List<Vector2Int>();
    }

    public int GetCellWidth()
    {
        return cellNumberWidth;
    }
    public int GetCellHeight()
    {
        return cellNumberHeight;
    }

    public bool IsCellEnabled(int x, int y)
    {
        return (enabledCells[x,y] == 1);
    }

    public string GetCharFieldSpriteByName(string name, int y_coord)
    {
        foreach (CharData cd in characters)
            if (cd.name == name)
                if (y_coord < cellNumberHeight/2) //if upper half of field
                    return cd.sprite_front;
                else
                    return cd.sprite_back;
        return null;
    }

    public CharData GetCharacterByName(string name)
    {
        foreach (CharData cd in characters)
            if (cd.name == name)
                return cd;
        return null;
    }

    public CharData[] GetCharactersAll()
    {
        return characters;
    }

    public int GetCharactersCount()
    {
        return characters.Length;
    }

    public PersonData GetPersonByCoords(Vector2Int coords)
    {
        return (persons[coords.x, coords.y]);
    }

    public PersonData GetPersonActive()
    {
        return (persons[ActivePersonCoords.x, ActivePersonCoords.y]);
    }

    public void SelectActiveCell(Vector2Int coords)
    {
        ActiveCellCoords = coords;
    }

    public Vector2Int GetActiveCell()
    {
        return ActiveCellCoords;
    }

    public void DeselectActiveCell()
    {
        SelectActiveCell(new Vector2Int(-1, -1));
    }

    public bool IsCellActive(Vector2Int coords)
    {
        return (ActiveCellCoords == coords);
    }

    public void PlacePersonToCellAndMakeActive(Vector2Int coords, string char_name)
    {
        persons[coords.x, coords.y] = new PersonData(GetCharacterByName(char_name));
        SelectActivePerson(coords);
    }

    public void RemovePersonFromCell(Vector2Int coords)
    {
        persons[coords.x, coords.y] = null;
    }

    public void PlacePersonToCellByData(Vector2Int coords, PersonData pd)
    {
        persons[coords.x, coords.y] = pd;
    }

    public bool MoveActiveCharacterToActiveCell()
    {
        if (ActivePersonCoords != new Vector2Int(-1,-1))
        {
            PersonData buffer = persons[ActivePersonCoords.x, ActivePersonCoords.y];
            persons[ActivePersonCoords.x, ActivePersonCoords.y] = null;
            persons[ActiveCellCoords.x, ActiveCellCoords.y] = buffer;
            SelectActivePerson(ActiveCellCoords);
            DeselectActiveCell();
            return true;
        }
        else
            return false;
    }

    public bool IsAnyPersonInThisCell(Vector2Int coords)
    {
        return (persons[coords.x, coords.y] != null);
    }

    public bool IsAnyPersonInActiveCell()
    {
        return IsAnyPersonInThisCell(ActiveCellCoords);
    }

    public bool IsActivePersonInThisCell(Vector2Int coords)
    {
        return (ActivePersonCoords == coords);
    }

    public bool IsActivePersonInActiveCell()
    {
        return (ActivePersonCoords == ActiveCellCoords);
    }

    public void SelectActivePerson(Vector2Int coords)
    {
        ActivePersonCoords = coords;
    }

    public void SelectActivePersonInActiveCell()
    {
        ActivePersonCoords = ActiveCellCoords;
    }

    public void SetActiveCellToActivePerson()
    {
        ActiveCellCoords = ActivePersonCoords;
    }

    public void DeselectActivePerson()
    {
        SelectActivePerson(new Vector2Int(-1, -1));
    }

    public CharData GetActivePersonCharData()
    {
        return persons[ActivePersonCoords.x, ActivePersonCoords.y].charData;
    }

    public Vector2Int GetActivePersonCoords()
    {
        return ActivePersonCoords;
    }

    public bool IsActivePersonExist()
    {
        return (ActivePersonCoords != new Vector2Int(-1, -1));
    }
    public bool IsActiveCellExist()
    {
        return (ActiveCellCoords != new Vector2Int(-1, -1));
    }

    public void FillMovementPointsForActivePerson()
    {
        List<Vector2Int> movement_points = new List<Vector2Int>();
        if (IsActivePersonExist())
            for (int i = 0; i < cellNumberWidth; i++)
                for (int j = 0; j < cellNumberHeight; j++)
                    if (IsCellEnabled(i, j) && !IsAnyPersonInThisCell(new Vector2Int(i, j))) //if cell enabled & doesn't contain character
                        //calculate distance between i,j & the active cell coords and compare with the speed of the active person
                        if (IsActivePersonCanGoToPoint(i, j))
                            //if (!IsAnyPersonInThisCell(new Vector2Int(i, j)))
                            movement_points.Add(new Vector2Int(i, j));
        movementPotentialCells = movement_points;
    }

    public void FillAttackPointsForActivePerson()
    {
        List<Vector2Int> attack_points = new List<Vector2Int>();
        if (IsActivePersonExist())
            for (int i = 0; i < cellNumberWidth; i++)
                for (int j = 0; j < cellNumberHeight; j++)
                    if (IsCellEnabled(i, j))
                        if (GetActiveCell() != new Vector2Int(i,j))
                            //calculate distance between i,j & the active cell coords and compare with the attack radius of the active person
                            if (IsActivePersonCanAttackPoint(i, j))
                                attack_points.Add(new Vector2Int(i, j));
        attackPotentialCells = attack_points;
    }

    public void FillSkillPointsForActivePerson()
    {
        List<Vector2Int> skill_points = new List<Vector2Int>();
        if (IsActivePersonExist())
            for (int i = 0; i < cellNumberWidth; i++)
                for (int j = 0; j < cellNumberHeight; j++)
                    if (IsCellEnabled(i, j))
                        if (GetActiveCell() != new Vector2Int(i, j))
                            //calculate distance between i,j & the active cell coords and compare with the attack radius of the active person
                            if (IsActivePersonCanUseSkillOnPoint(i, j))
                                skill_points.Add(new Vector2Int(i, j));
        skillPotentialCells = skill_points;
    }

    public void FillAllPointsForActivePerson()
    {
        FillMovementPointsForActivePerson();
        FillAttackPointsForActivePerson();
        FillSkillPointsForActivePerson();
    }

    public List<Vector2Int> GetMovementPoints()
    {
        return movementPotentialCells;
    }

    public List<Vector2Int> GetAttackPoints()
    {
        return attackPotentialCells;
    }

    public List<Vector2Int> GetSkillPoints()
    {
        return skillPotentialCells;
    }

    public void ClearMovementPoints()
    {
        movementPotentialCells.Clear();
    }

    public void ClearAttackPoints()
    {
        attackPotentialCells.Clear();
    }

    public void ClearSkillPoints()
    {
        skillPotentialCells.Clear();
    }

    static int distanceBetweenCells(int x1, int y1, int x2, int y2)
    {
        Vector3Int evenr_to_cube(int row, int col)
        {
            int q = col - ((row + (row % 2))/2);
            int r = row;
            return new Vector3Int(q, r, -q - r);
        }
        Vector3Int point_1 = evenr_to_cube(x1, y1);
        Vector3Int point_2 = evenr_to_cube(x2, y2);
        int a = (int)Math.Abs(point_1.x - point_2.x);
        int b = (int)Math.Abs(point_1.y - point_2.y);
        int c = (int)Math.Abs(point_1.z - point_2.z);
        return Math.Max(a, Math.Max(b, c));
    }

    bool IsActivePersonCanGoToPoint(int x, int y)
    {
        int distance = distanceBetweenCells(x, y, ActivePersonCoords.x, ActivePersonCoords.y);
        return ((distance <= GetActivePersonCharData().speed) && (distance > 0)); ;
    }

    public bool IsActivePersonCanGoToActiveCell()
    {
        return IsActivePersonCanGoToPoint(ActiveCellCoords.x, ActiveCellCoords.y);
    }

    public bool IsActivePersonInUpperHalfOfField()
    {
        return (ActivePersonCoords.y < cellNumberHeight / 2);
    }

    bool IsActivePersonCanAttackPoint(int x, int y)
    {
        int distance = distanceBetweenCells(x, y, ActivePersonCoords.x, ActivePersonCoords.y);
        CharData cd = GetActivePersonCharData();
        return ((distance <= cd.attack_radius) && (distance >= cd.attack_radius_min));
    }

    public bool IsActivePersonCanAttackActiveCell()
    {
        return IsActivePersonCanAttackPoint(ActiveCellCoords.x, ActiveCellCoords.y);
    }

    bool IsActivePersonCanUseSkillOnPoint(int x, int y)
    {
        int distance = distanceBetweenCells(x, y, ActivePersonCoords.x, ActivePersonCoords.y);
        CharData cd = GetActivePersonCharData();
        return ((distance <= cd.skill_radius) && (distance > 0));
    }

    public bool IsActivePersonCanUseSkillOnActiveCell()
    {
        return IsActivePersonCanUseSkillOnPoint(ActiveCellCoords.x, ActiveCellCoords.y);
    }

    public int AttackPersonInActiveCellByActivePerson(out bool is_crit, out bool damage_increased)
    {
        var rand = new System.Random();
        Vector2Int act_cell_coords = GetActiveCell();
        PersonData personDataTarget = GetPersonByCoords(act_cell_coords);
        PersonData personDataAttacker = GetPersonActive();
        int dmgPercentIncrease = 0;
        bool isHalfDefenseIgnore = false;
        switch (personDataAttacker.charData.passive_skill.ToLower())
        {
            case "святой воин":
                if (new[] { "Гоблин" }.Contains(personDataTarget.charData.race))
                    dmgPercentIncrease = 20;
                break;
            case "святое слово":
                if (new[] { "Гоблин" }.Contains(personDataTarget.charData.race))
                    dmgPercentIncrease = 17;
                break;
            case "знание зверя":
                if (new[] { "Животное" }.Contains(personDataTarget.charData.race))
                    dmgPercentIncrease = 10;
                break;
            case "недооценка противником":
                isHalfDefenseIgnore = true;
                break;
            case "знакомый враг":
                if (new[] { "Эльфийский оракул Эльтиар", "Паук Сквернолесья", "Магистр Ордена Ольтродор", "Вгхерг Вождь Желторуких" }.Contains(personDataTarget.charData.name))
                    dmgPercentIncrease = 10;
                break;
            case "корректировка":
                if (IsStrikedPersonLastStrikedToo())
                {
                    dmgPercentIncrease = 20;
                    ClearLastStrikedPersonIdOfActivePerson();
                }
                else
                {
                    SetLastStrikedPersonIdOfActivePerson();
                };
                break;
        }

        bool is_critical = ((rand.Next(100) - personDataAttacker.charData.critical_percent) < 0);
        float mult_def = isHalfDefenseIgnore ? 0.5f : 1f;
        //attack formula - например атака 100, у атакованного защита 15%, урон будет 85. крит это двойной урон
        int damage = (int)(((float)(100f - mult_def * personDataTarget.charData.defense_percent) / 100) * personDataAttacker.charData.attack * (is_critical ? 2 : 1));
        damage = (int)(damage * ((float)(100f + dmgPercentIncrease) / 100));
        persons[act_cell_coords.x, act_cell_coords.y].currentHealth -= damage;

        is_crit = is_critical;
        damage_increased = ((dmgPercentIncrease > 0) || isHalfDefenseIgnore );
        return damage;
    }

    public bool IsPersonInActiveCellAlive()
    {
        Vector2Int act_cell_coords = GetActiveCell();
        return (persons[act_cell_coords.x, act_cell_coords.y].currentHealth > 0);
    }

    public void IncreaseInternalTurnCounterOfActivePerson()
    {
        persons[ActivePersonCoords.x, ActivePersonCoords.y].internalTurnCounter++;
    }

    public void SetLastStrikedPersonIdOfActivePerson()
    {
        persons[ActivePersonCoords.x, ActivePersonCoords.y].lastStrikedPersonId = persons[ActiveCellCoords.x, ActiveCellCoords.y].uniqueSpawnId;
    }

    public void ClearLastStrikedPersonIdOfActivePerson()
    {
        persons[ActivePersonCoords.x, ActivePersonCoords.y].lastStrikedPersonId = 0;
    }

    public bool IsStrikedPersonLastStrikedToo()
    {
        return (persons[ActivePersonCoords.x, ActivePersonCoords.y].lastStrikedPersonId == persons[ActiveCellCoords.x, ActiveCellCoords.y].uniqueSpawnId);
    }

    public int HealActivePersonByHP(int hp)
    {
        int new_hp = persons[ActivePersonCoords.x, ActivePersonCoords.y].currentHealth + hp;
        if (new_hp > persons[ActivePersonCoords.x, ActivePersonCoords.y].charData.health)
            new_hp = persons[ActivePersonCoords.x, ActivePersonCoords.y].charData.health;
        int res = new_hp - persons[ActivePersonCoords.x, ActivePersonCoords.y].currentHealth;
        persons[ActivePersonCoords.x, ActivePersonCoords.y].currentHealth = new_hp;
        return res;
    }
}
