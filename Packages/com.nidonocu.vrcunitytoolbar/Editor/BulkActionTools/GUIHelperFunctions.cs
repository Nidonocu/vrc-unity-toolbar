#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    }
}
#endif