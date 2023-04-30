using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Tomatech.RePalette
{
    public static class RepaletteResourceManager
    {
        static ThemeFilterBase themeFilter;
        public static void SetThemeFilter(ThemeFilterBase newThemeFilter) => themeFilter = newThemeFilter;
        public static ThemeFilterBase ThemeFilter => themeFilter;
        public static T GetThemeFilter<T>() where T:ThemeFilterBase => themeFilter as T;

        public static event Action onThemeUpdateCompleted;

        static List<IThemeable> registeredThemeables = new();

        //public static void ClearRegistry()
        //{
        //    registeredThemeables.Clear();
        //}

        public static void RegisterThemeable(IThemeable toAdd)
        {
            if (!registeredThemeables.Contains(toAdd))
                registeredThemeables.Add(toAdd);
        }

        public static void UnregisterThemeable(IThemeable toRemove)
        {
            if (registeredThemeables.Contains(toRemove))
                registeredThemeables.Remove(toRemove);
        }

        public static async Task UpdateRegisteredThemeables()
        {
            List<Task> tasks = new();
            for (int i = 0; i < registeredThemeables.Count; i++)
            {
                tasks.Add(registeredThemeables[i].UpdateThemeContent());
            }
            Debug.Log("waiting for themeables to finish updating");
            await Task.WhenAll(tasks);
            onThemeUpdateCompleted?.Invoke();
            Debug.Log("all themeables have updated");
        }
    }

    public interface IThemeable
    {
        public Task UpdateThemeContent();
    }
}
