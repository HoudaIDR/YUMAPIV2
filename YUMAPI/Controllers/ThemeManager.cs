// ============================================================
//  Controllers/ThemeManager.cs
//  Gestion de la couleur accent - mise à jour globale via App.Resources
// ============================================================

using System.Windows;
using System.Windows.Media;

namespace YUMAPI.Controllers
{
    public static class ThemeManager
    {
        public static string CouleurHex = "#FF6B35";

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
            CouleurHex = hex;

            // Mettre à jour la ressource globale → tous les éléments se rafraîchissent
            if (Application.Current != null)
            {
                Color couleur = (Color)ColorConverter.ConvertFromString(hex);
                Application.Current.Resources["AccentColor"] = new SolidColorBrush(couleur);
            }
        }
    }
}