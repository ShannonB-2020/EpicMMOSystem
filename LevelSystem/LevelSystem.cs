using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicMMOSystem;

public enum Parameter
{
    Strength = 0, Agility = 1, Intellect = 2, Body = 3
}
public partial class LevelSystem
{
    #region Singlton
    private static LevelSystem instance;
    public static LevelSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new LevelSystem();
                return instance;
            }
            return instance;
        }
    }
    #endregion

    private Dictionary<int, long> levelsExp;
    private string pluginKey = EpicMMOSystem.ModName;
    private const string midleKey = "LevelSystem";
    private int[] depositPoint = { 0, 0, 0, 0 };

    public LevelSystem()
    {
        FillLevelsExp();
    }
    
    public int getLevel()
    {
        if (!Player.m_localPlayer.m_knownTexts.ContainsKey($"{pluginKey}_{midleKey}_Level"))
        {
            return 1;
        }
        return int.Parse(Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_Level"]);
    }
    
    private void setLevel(int value)
    {
        Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_Level"]= value.ToString();
    }
    
    public long getCurrentExp()
    {
        if (!Player.m_localPlayer.m_knownTexts.ContainsKey($"{pluginKey}_{midleKey}_CurrentExp"))
        {
            return 0;
        }
        return int.Parse(Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_CurrentExp"]);
    }
    
    private void setCurrentExp(long value)
    {
        Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_CurrentExp"]= value.ToString();
    }
    
    private void setParameter(Parameter parameter, int value)
    {
        int setValue = Math.Max(0, value);
        Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_{parameter.ToString()}"] = setValue.ToString();
    }
    
    public int getParameter(Parameter parameter)
    {
        if (!Player.m_localPlayer.m_knownTexts.ContainsKey($"{pluginKey}_{midleKey}_{parameter.ToString()}"))
        {
            return 0;
        }
        return int.Parse(Player.m_localPlayer.m_knownTexts[$"{pluginKey}_{midleKey}_{parameter.ToString()}"]);
    }

    public int getFreePoints()
    {
        var levelPoint = EpicMMOSystem.freePointForLevel.Value;
        var freePoint = EpicMMOSystem.startFreePoint.Value;
        var level = getLevel();
        var total = level * levelPoint + freePoint;
        int usedUp = 0;
        for (int i = 0; i < 4; i++)
        {
            usedUp += getParameter((Parameter)i);
        }
        return total - usedUp;
    }

    public void addPointsParametr(Parameter parameter, int addPoint)
    {
        var freePoint = getFreePoints();
        if (!(freePoint > 0)) return;
        var applyPoint = Mathf.Clamp(addPoint, 1, freePoint);
        depositPoint[(int)parameter] += applyPoint;
        var currentPoint = getParameter(parameter);
        setParameter(parameter, currentPoint + applyPoint);
    }
    
    public long getNeedExp(int addLvl = 0)
    {
        var lvl = Mathf.Clamp(getLevel() + 1 + addLvl, 1, EpicMMOSystem.maxLevel.Value);
        return levelsExp[lvl];
    }
    
    public void ResetAllParameter()
    {
        for (int i = 0; i < 4; i++)
        {
            setParameter((Parameter)i, 0);
        }
        MyUI.UpdateParameterPanel();
    }

    public void ResetAllParameterPayment()
    {
        var price = getPriceResetPoints();
        var currentCoins = Player.m_localPlayer.m_inventory.CountItems("$item_coins");
        if (currentCoins < price) return;
        Player.m_localPlayer.m_inventory.RemoveItem("$item_coins", price);
        ResetAllParameter();
    }

    public void AddExp(int exp)
    {
        if(exp < 1) return;
        var current = getCurrentExp();
        var need = getNeedExp();
        current += exp;
        int addLvl = 0;
        while (current > need)
        {
            current -= need;
            addLvl++;
            need = getNeedExp(addLvl);
        }
        AddLevel(addLvl);
        setCurrentExp(current);
        MyUI.updateExpBar();
        Player.m_localPlayer.Message(
            MessageHud.MessageType.TopLeft, 
            $"{(EpicMMOSystem.localization["$get_exp"])}: {exp}"
        );
    }

    public void AddLevel(int count)
    {
        if (count <= 0) return;
        var current = getLevel();
        current += count;
        setLevel(Mathf.Clamp(current,1, EpicMMOSystem.maxLevel.Value));
        PlayerFVX.levelUp();
    }

    public bool hasDepositPoints()
    {
        bool result = false;
        foreach (var deposit in depositPoint)
        {
            if (deposit > 0)
            {
                result = true;
                break;
            }
        }
        return result;
    }

    public void applyDepositPoints()
    {
        for (int i = 0; i < depositPoint.Length; i++)
        {
            depositPoint[i] = 0;
        }
        MyUI.UpdateParameterPanel();
    }
    
    public void cancelDepositPoints()
    {
        if (!Player.m_localPlayer) return;
        for (int i = 0; i < depositPoint.Length; i++)
        {
            if (depositPoint[i] == 0) continue;
            var parameter = (Parameter)i;
            var deposit = depositPoint[i];
            var point = getParameter(parameter);
            setParameter(parameter, point - deposit);
            depositPoint[i] = 0;
        }
        MyUI.UpdateParameterPanel();
    }

    public int getPriceResetPoints()
    {
        var count = 0;
        for (int i = 0; i < 4; i++)
        {
            count += getParameter((Parameter)i);
        }
        count *= EpicMMOSystem.priceResetPoints.Value;
        return count;
    }
    
    //FillLevelExp
    private void FillLevelsExp()
    {
        var levelExp = EpicMMOSystem.levelExp.Value;
        var multiply =  EpicMMOSystem.multiNextLevel.Value;
        var maxLevel =  EpicMMOSystem.maxLevel.Value;
        levelsExp = new ();
        long current = 0;
        for (int i = 1; i <= maxLevel; i++)
        {
            current = (long) Math.Round(current * multiply + levelExp);
            levelsExp[i + 1] = current;
        }
    }
}