using System;
using System.Runtime.InteropServices;
using UnityEngine;

using GorillaLuau.Helpers;

namespace GorillaLuau.Lua
{
    public unsafe class Helper
    {
        public static unsafe void PushGameObject(lua_State* state, GameObject obj)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(obj);
            Luau.lua_pushlightuserdatatagged(state, (void*)ptr, 0);
        }

        public static unsafe GameObject CheckGameObject(lua_State* state, int index)
        {
            void* ptr = Luau.lua_touserdata(state, index);
            if (ptr == null) return null;
            return Marshal.GetObjectForIUnknown((IntPtr)ptr) as GameObject;
        }

        public static unsafe void PushComponent(lua_State* state, Component comp)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(comp);
            Luau.lua_pushlightuserdatatagged(state, (void*)ptr, 0);
        }

        public static unsafe object CheckObject(lua_State* state, int index)
        {
            void* ptr = Luau.lua_touserdata(state, index);
            if (ptr == null) return null;
            return Marshal.GetObjectForIUnknown((IntPtr)ptr);
        }

        public static unsafe void PushObject(lua_State* L, UnityEngine.Object obj)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(obj);
            Luau.lua_pushlightuserdatatagged(L, (void*)ptr, 0);
        }

        public static void PushValue(lua_State* L, object value)
        {
            if (value == null)
                Luau.lua_pushnil(L);
            else if (value is int i)
                Luau.lua_pushnumber(L, i);
            else if (value is float f)
                Luau.lua_pushnumber(L, f);
            else if (value is double d)
                Luau.lua_pushnumber(L, d);
            else if (value is bool b)
                Luau.lua_pushboolean(L, b ? 1 : 0);
            else if (value is string s)
                Luau.lua_pushstring(L, s);
            else if (value is Vector3 v)
            {
                Luau.lua_createtable(L, 0, 3);
                Luau.lua_pushnumber(L, v.x); Luau.lua_setfield(L, -2, "x");
                Luau.lua_pushnumber(L, v.y); Luau.lua_setfield(L, -2, "y");
                Luau.lua_pushnumber(L, v.z); Luau.lua_setfield(L, -2, "z");
            }
            else if (value is Color c)
            {
                Luau.lua_createtable(L, 0, 4);
                Luau.lua_pushnumber(L, c.r); Luau.lua_setfield(L, -2, "r");
                Luau.lua_pushnumber(L, c.g); Luau.lua_setfield(L, -2, "g");
                Luau.lua_pushnumber(L, c.b); Luau.lua_setfield(L, -2, "b");
                Luau.lua_pushnumber(L, c.a); Luau.lua_setfield(L, -2, "a");
            }
            else if (value is GameObject go)
                PushGameObject(L, go);
            else if (value is Component comp)
                PushComponent(L, comp);
            else if (value is Material mat)
                PushObject(L, mat);
            else
            {
                myLogger.LogError($"Unsupported type for Lua push: {value.GetType()}");
                Luau.lua_pushstring(L, value.ToString());
            }
        }

        public static object ConvertLuaTableValue(lua_State* state, int index, Type type, string propName)
        {
            int luaType = Luau.lua_type(state, index);
            sbyte* strPtr;

            switch (luaType)
            {
                case 0: return null;
                case 1: return Luau.lua_toboolean(state, index) != 0;
                case 2:
                case 3:
                    double number = Luau.lua_tonumber(state, index);
                    if (propName == "layer" || propName.Contains("Count")) return (int)number;
                    return number;
                case 4:
                    strPtr = Luau.lua_tostring(state, index);
                    return strPtr != null ? Marshal.PtrToStringAnsi((IntPtr)strPtr) : "";
                case 5:
                    if (type == typeof(string) || propName.ToLower().Contains("name"))
                    {
                        strPtr = Luau.lua_tostring(state, index);
                        if (strPtr != null) return Marshal.PtrToStringAnsi((IntPtr)strPtr);
                        return "";
                    }
                    return LuaTableToType(state, index, type, propName);
                case 6:
                    if (propName.ToLower().Contains("position") || propName.ToLower().Contains("scale") || propName.ToLower().Contains("rotation"))
                        return LuaTableToVector3(state, index); // fallback for Vector3
                    if (propName.ToLower().Contains("color"))
                        return LuaTableToColor(state, index);   // fallback for Color
                    return CheckObject(state, index);
                default:
                    myLogger.LogError($"Unsupported Lua type {luaType} for property '{propName}'");
                    return null;
            }
        }

        public static object LuaTableToType(lua_State* state, int index, Type type, string propName)
        {
            if (propName == "position" || propName == "localPosition" ||
                propName == "scale" || propName == "localScale" ||
                propName == "eulerAngles" || propName.Contains("position") ||
                propName.Contains("scale") || propName.Contains("rotation"))
            {
                return LuaTableToVector3(state, index);
            }

            if (propName == "color" || propName.Contains("color") || propName.Contains("Color"))
            {
                return LuaTableToColor(state, index);
            }

            myLogger.LogError($"Unhandled table type for property '{propName}'");
            return null;
        }

        private static Vector3 LuaTableToVector3(lua_State* state, int index)
        {
            float x = 0, y = 0, z = 0;

            Luau.lua_getfield(state, index, "x");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                x = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            Luau.lua_getfield(state, index, "y");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                y = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            Luau.lua_getfield(state, index, "z");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                z = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            return new Vector3(x, y, z);
        }

        private static Color LuaTableToColor(lua_State* state, int index)
        {
            float r = 0, g = 0, b = 0, a = 1;

            Luau.lua_getfield(state, index, "r");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                r = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            Luau.lua_getfield(state, index, "g");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                g = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            Luau.lua_getfield(state, index, "b");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                b = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            Luau.lua_getfield(state, index, "a");
            if (Luau.lua_type(state, -1) == 3) // LUA_TNUMBER
                a = (float)Luau.lua_tonumber(state, -1);
            Luau.lua_pop(state, 1);

            return new Color(r, g, b, a);
        }

        public static bool CanConvert(object value, Type targetType)
        {
            if (value == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            if (targetType.IsInstanceOfType(value))
                return true;

            if (targetType.IsEnum && value is int)
                return true;
            if (targetType.IsEnum && value is double)
                return true;

            if (targetType == typeof(int) && value is double)
                return true;
            if (targetType == typeof(float) && value is double)
                return true;
            if (targetType == typeof(double) && value is int)
                return true;
            if (targetType == typeof(bool) && value is int)
                return true;

            return false;
        }

        public static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType.IsEnum)
            {
                if (value is int intValue)
                    return Enum.ToObject(targetType, intValue);
                if (value is double doubleValue)
                    return Enum.ToObject(targetType, (int)doubleValue);
            }

            if (targetType == typeof(int) && value is double d)
                return (int)d;
            if (targetType == typeof(float) && value is double d2)
                return (float)d2;
            if (targetType == typeof(double) && value is int i)
                return (double)i;
            if (targetType == typeof(bool) && value is int i2)
                return i2 != 0;
            if (targetType == typeof(string))
                return value.ToString();

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }

        public static object ConvertLuaValue(lua_State* L, int index, Type targetType)
        {
            int type = Luau.lua_type(L, index);

            switch (type)
            {
                case 0: // LUA_TNIL
                    return null;
                case 1: // LUA_TBOOLEAN
                    return Luau.lua_toboolean(L, index) != 0;
                case 3: // LUA_TNUMBER
                    return Luau.lua_tonumber(L, index);
                case 4: // LUA_TSTRING
                    sbyte* strPtr = Luau.lua_tostring(L, index);
                    return strPtr != null ? Marshal.PtrToStringAnsi((IntPtr)strPtr) : null;
                case 2: // LUA_TUSERDATA
                    return CheckObject(L, index);
                default:
                    return null;
            }
        }
    }
}
