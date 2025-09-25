#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.BulkActionTools
{
    public static class GUIHelperFunctions
    {
        private static Texture NidoLogo;

        public static void DrawFooter()
        {
            if (NidoLogo == null)
            {
                NidoLogo = Resources.Load<Texture>("nido_logo_small");
            }

            GUILayout.FlexibleSpace();

            var footerStyle = EditorStyles.miniLabel;

            GUILayout.Label(new GUIContent(" Part of the VRC Unity Toolbar by Nidonocu", NidoLogo),
                footerStyle, GUILayout.MaxHeight(32f));
        }

        public static bool RenderResultsList<T>(
            bool displayStatus,
            string label,
            ref Vector2 scrollPosition,
            List<T> foundItems
            ) where T : UnityEngine.Object
        {
            var display = EditorGUILayout.BeginFoldoutHeaderGroup(displayStatus, label);
            if (display)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                EditorGUI.BeginDisabledGroup(true);
                foreach (var item in foundItems)
                {
                    EditorGUILayout.ObjectField(item, typeof(T), false);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndScrollView();
            }
            return display;
        }
    }
}
#endif