using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class Extensions
{
    public static T UseDictionary<T, T1, T2>(this Func<T1[], T2[], T> func, IDictionary<T1, T2> dict)
    {
        return func.Invoke(dict.Keys.ToArray(), dict.Values.ToArray());
    }

    /// <summary>
    /// Will return the first component of given type on this gameObject, or will create one if it doesn't exist;
    /// </summary>
    /// <param name="gameObject"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GuaranteeGetComponent<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent<T>(out var component)) return component;
        return gameObject.AddComponent<T>();
    }

    public static Component GuaranteeGetComponent(this GameObject gameObject, Type T)
    {
        if (gameObject.TryGetComponent(T, out var component)) return component;
        return gameObject.AddComponent(T);
    }
} 
