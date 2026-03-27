// ============================================================
//  Controllers/ThemeManager.cs
//  Gère la couleur accent de l'application
//  Quand on change la couleur, toutes les vues sont notifiées
// ============================================================

using System;
using System.Windows.Media;

namespace YUMAPI.Controllers
{
    public static class ThemeManager
    {
        // Couleur par défaut : orange
        public static string CouleurHex = "#FF6B35";

        // Événement déclenché quand la couleur change
        // Toutes les vues peuvent s'y abonner pour se mettre à jour
        public static event Action CouleurChangee;

        // Propriété qui retourne une brosse de la couleur actuelle
        public static SolidColorBrush CouleurAccent
        {
            get
            {
                return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(CouleurHex)
                );
            }
        }

        // Changer la couleur et notifier tout le monde
        public static void ChangerCouleur(string hex)
        {
            CouleurHex = hex;

            // Déclencher l'événement si quelqu'un est abonné
            if (CouleurChangee != null)
                CouleurChangee();
        }
    }
}