// ============================================================
//  Controllers/ThemeManager.cs
//  Gère la couleur accent de l'application
//  Quand on change la couleur, toutes les vues sont notifiées
// ============================================================

using System;
using System.Windows;
using System.Windows.Media;

namespace YUMAPI.Controllers
{
    public static class ThemeManager
    {
        public static string CouleurHex = "#FF6B35";

        public static event Action CouleurChangee;

        public static SolidColorBrush CouleurAccent
        {
            get
            {
                return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(CouleurHex));
            }
        }

        public static void ChangerCouleur(string hex)
        {
            CouleurHex = (hex ?? "#FF6B35").Trim();

            if (Application.Current != null)
            {
                Color couleur = (Color)ColorConverter.ConvertFromString(CouleurHex);
                Application.Current.Resources["AccentColor"] = new SolidColorBrush(couleur);
            }

            if (CouleurChangee != null)
                CouleurChangee();
        }
    }
}