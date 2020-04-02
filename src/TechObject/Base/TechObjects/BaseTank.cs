﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechObject
{
    /// <summary>
    /// Базовый танк
    /// </summary>
    public class BaseTank : BaseTechObject
    {
        public BaseTank() : base()
        {
            S88Level = 1;
            Name = "Танк";
            EplanName = "tank";
            BaseOperations = DataBase.Imitation.TankOperations();
            BaseProperties = DataBase.Imitation.TankProperties();
            BasicName = "tank";
            Equipment = DataBase.Imitation.TankEquipment();
            AggregateProperties = DataBase.Imitation.EmptyAggregateProperties();
        }

        /// <summary>
        /// Клонировать объект
        /// </summary>
        /// <param name="techObject">Новый владелец базового объекта</param>
        /// <returns></returns>
        public override BaseTechObject Clone(TechObject techObject)
        {
            var cloned = DataBase.Imitation.BaseTechObjects()
                .Where(x => x.Name == this.Name)
                .FirstOrDefault();
            cloned.Owner = techObject;
            return cloned;
        }

        #region Сохранение в prg.lua
        /// <summary>
        /// Сохранить информацию об операциях объекта в prg.lua
        /// </summary>
        /// <param name="objName">Имя объекта для записи</param>
        /// <param name="prefix">Отступ</param>
        /// <returns></returns>
        public override string SaveToPrgLua(string objName,
            string prefix)
        {
            var res = "";
            res += SaveObjectInfoToPrgLua(objName, prefix);
            
            res += SaveOperations(objName, prefix);
            res += SaveOperationsSteps(objName, prefix);
            res += SaveOperationsParameters(objName);
            res += SaveEquipment(objName);

            return res;
        }

        /// <summary>
        /// Сохранить информацию об объекте в prg.lua
        /// </summary>
        /// <param name="objName">Имя объекта</param>
        /// <param name="prefix">Отступ</param>
        /// <returns></returns>
        private string SaveObjectInfoToPrgLua(string objName,
            string prefix)
        {
            var res = "";

            var objects = TechObjectManager.GetInstance();
            var masterObj = objects.Objects
                .Where(x => x.Name.Contains("Мастер")).FirstOrDefault();
            if (masterObj != null)
            {
                res += objName + ".master = prg." + masterObj.NameEplan
                    .ToLower() + masterObj.TechNumber + "\n";
            }

            // Параметры сбрасываемые до мойки.
            res += objName + ".reset_before_wash =\n" +
                prefix + "{\n" +
                prefix + objName + ".PAR_FLOAT.V_ACCEPTING_CURRENT,\n" +
                prefix + objName + ".PAR_FLOAT.PRODUCT_TYPE,\n" +
                prefix + objName + ".PAR_FLOAT.V_ACCEPTING_SET\n" +
                prefix + "}\n";

            res += "\n";

            return res;
        }
      
        /// <summary>
        /// Сохранить параметры операций базового объекта танк.
        /// </summary>
        /// <param name="objName">Имя объекта для записи</param>
        /// <returns></returns>
        private string SaveOperationsParameters(string objName)
        {
            var res = "";

            var modesManager = this.Owner.ModesManager;
            var modes = modesManager.Modes;

            foreach (Mode mode in modes)
            {
                var baseOperation = mode.BaseOperation;
                switch (baseOperation.Name)
                {
                    case "Мойка":
                        res += SaveWashOperationParameters(objName,
                            baseOperation);
                        break;

                    case "Наполнение":
                        res += SaveFillOperationParameters(objName,
                            baseOperation);
                        break;

                    default:
                        foreach (BaseProperty param in baseOperation.Properties)
                        {
                            if (param.CanSave())
                            {
                                string val = param
                                    .Value == "" ? "nil" : param.Value;
                                res += $"{objName}.{baseOperation.LuaName}." +
                                    $"{param.LuaName} = {val}\n";
                            }
                        }
                        break;
                }
            }

            return res;
        }

        /// <summary>
        /// Сохранить параметры операции мойки
        /// </summary>
        /// <param name="objName">Имя объекта</param>
        /// <param name="baseOperation">Базовая операция</param>
        /// <returns></returns>
        private string SaveWashOperationParameters(string objName,
            BaseOperation baseOperation)
        {
            var res = "";

            foreach (BaseProperty param in baseOperation.Properties)
            {
                if (param.CanSave())
                {
                    string val = param.Value == "" ? "nil" : param.Value;
                    if (val != "nil")
                    {
                        res += objName + "." + param.LuaName +
                            " = prg.control_modules." + val + "\n";
                    }
                    else
                    {
                        res += objName + "." + param.LuaName +
                            " = " + val + "\n";
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Сохранить параметры операции наполнения.
        /// </summary>
        /// <param name="objName">Имя объекта</param>
        /// <param name="baseOperation">Базовая операция</param>
        /// <returns></returns>
        private string SaveFillOperationParameters(string objName,
            BaseOperation baseOperation)
        {
            var res = "";

            foreach (BaseProperty param in baseOperation.Properties)
            {
                if (param.CanSave())
                {
                    string val = param.Value == "" ? "nil" : param.Value;

                    switch (param.LuaName)
                    {
                        case "OPERATION_AFTER_FILL":
                            var modes = this.Owner.ModesManager.Modes;
                            var mode = modes
                                .Where(x => x.GetModeNumber().ToString() == val)
                                .FirstOrDefault();

                            if (mode != null)
                            {
                                val = mode.BaseOperation.LuaName.ToUpper();
                            }

                            if (val != "nil")
                            {
                                res += objName + "." + param.LuaName +
                                $" = {objName}.operations." + val + "\n";
                            }
                            else
                            {
                                res += objName + "." + param.LuaName +
                                    $" = {val}\n";
                            }
                            break;

                        default:
                            res += $"{objName}.{baseOperation.LuaName}." +
                                    $"{param.LuaName} = {val}\n";
                            break;
                    }
                }
            }

            return res;
        }
        #endregion
    }
}
